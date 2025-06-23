using Application.Common.Interfaces;
using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.Commands
{
    public record RescheduleAppointmentCommand : IRequest<Guid>
    {
        public Guid AppointmentId { get; init; }
        public DateTime NewDate { get; init; }
        public TimeOnly NewStartTime { get; init; }
        public int DurationMinutes { get; init; }
    }

    public class RescheduleAppointmentValidator : AbstractValidator<RescheduleAppointmentCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;

        public RescheduleAppointmentValidator(IAppDbContext context, IDateTimeService dateTimeService)
        {
            _context = context;
            _dateTimeService = dateTimeService;

            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Appointment ID is required")
                .MustAsync(AppointmentExistsAndCanBeRescheduled)
                .WithMessage("Appointment not found or cannot be rescheduled");

            RuleFor(x => x.NewDate)
                .Must(BeAtLeast24HoursInAdvance)
                .WithMessage("Appointments must be rescheduled at least 24 hours in advance")
                .Must(BeWithin3Months)
                .WithMessage("Cannot reschedule more than 3 months ahead");

            RuleFor(x => x.DurationMinutes)
                .Must(HaveValidDuration)
                .WithMessage("Duration must be 15, 30, 45, or 60 minutes");

            RuleFor(x => x)
                .MustAsync(BeWithinWorkingHours)
                .WithMessage("Selected time is outside working hours")
                .MustAsync(NotConflictWithExisting)
                .WithMessage("This time slot is already booked");
        }

        private async Task<bool> AppointmentExistsAndCanBeRescheduled(Guid appointmentId, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment == null) return false;

            return appointment.Status == AppointmentStatus.Scheduled &&
                   appointment.AppointmentDateTime > _dateTimeService.UtcNow.AddHours(24);
        }

        private bool BeAtLeast24HoursInAdvance(DateTime appointmentDate)
        {
            var appointmentDateTime = appointmentDate.Date;
            var minimumDate = _dateTimeService.UtcNow.AddHours(24).Date;
            return appointmentDateTime >= minimumDate;
        }

        private bool BeWithin3Months(DateTime appointmentDate)
        {
            var maximumDate = _dateTimeService.UtcNow.Date.AddMonths(3);
            return appointmentDate.Date <= maximumDate;
        }

        private static bool HaveValidDuration(int duration)
        {
            var validDurations = new[] { 15, 30, 45, 60 };
            return validDurations.Contains(duration);
        }

        private async Task<bool> BeWithinWorkingHours(RescheduleAppointmentCommand command, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken);

            if (appointment == null) return false;

            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(wh => wh.ProviderId == appointment.ProviderId &&
                                         wh.DayOfWeek == command.NewDate.DayOfWeek &&
                                         wh.IsActive, cancellationToken);

            if (workingHours == null) return false;

            var endTime = command.NewStartTime.AddMinutes(command.DurationMinutes);
            return command.NewStartTime >= workingHours.StartTime && endTime <= workingHours.EndTime;
        }

        private async Task<bool> NotConflictWithExisting(RescheduleAppointmentCommand command, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken);

            if (appointment == null) return false;

            var requestedTimeSlot = TimeSlot.FromStartAndDuration(command.NewStartTime, command.DurationMinutes);

            var hasConflict = await _context.Appointments
                .Where(a => a.ProviderId == appointment.ProviderId &&
                           a.Id != command.AppointmentId &&
                           a.AppointmentDate.Date == command.NewDate.Date &&
                           a.Status == AppointmentStatus.Scheduled)
                .AnyAsync(a => a.StartTime < requestedTimeSlot.EndTime && a.EndTime > requestedTimeSlot.StartTime,
                         cancellationToken);

            return !hasConflict;
        }
    }
    public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<RescheduleAppointmentCommandHandler> _logger;

        public RescheduleAppointmentCommandHandler(
            IAppDbContext context,
            IEmailService emailService,
            IDateTimeService dateTimeService,
            ILogger<RescheduleAppointmentCommandHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Guid> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Rescheduling appointment {AppointmentId} to {NewDate} at {NewTime}",
                request.AppointmentId, request.NewDate, request.NewStartTime);

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

                if (appointment == null)
                    throw new BusinessRuleException("Appointment not found");

                var newTimeSlot = TimeSlot.FromStartAndDuration(request.NewStartTime, request.DurationMinutes);
                var currentUtcTime = _dateTimeService.UtcNow;

                await EnsureNoConflictsAsync(appointment.ProviderId, request.NewDate, newTimeSlot, request.AppointmentId, cancellationToken);

                appointment.Reschedule(request.NewDate, newTimeSlot, currentUtcTime);

                await _context.SaveChangesAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendConfirmationEmailAsync(appointment, cancellationToken);
                        _logger.LogInformation("Reschedule confirmation email sent for appointment {AppointmentId}", appointment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send reschedule confirmation email for appointment {AppointmentId}", appointment.Id);
                    }
                }, cancellationToken);

                _logger.LogInformation("Successfully rescheduled appointment {AppointmentId}", request.AppointmentId);
                return appointment.Id;
            }
            catch (BusinessRuleException)
            {
                _logger.LogWarning("Business rule violation when rescheduling appointment {AppointmentId}", request.AppointmentId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", request.AppointmentId);
                throw new InvalidOperationException("Failed to reschedule appointment. Please try again.", ex);
            }
        }

        private async Task EnsureNoConflictsAsync(Guid providerId, DateTime appointmentDate, TimeSlot timeSlot, Guid excludeAppointmentId, CancellationToken cancellationToken)
        {
            var conflictExists = await _context.Appointments
                .Where(a => a.ProviderId == providerId
                        && a.Id != excludeAppointmentId
                        && a.AppointmentDate.Date == appointmentDate.Date
                        && a.Status == AppointmentStatus.Scheduled)
                .AnyAsync(a => a.StartTime < timeSlot.EndTime && a.EndTime > timeSlot.StartTime, cancellationToken);

            if (conflictExists)
                throw new BusinessRuleException("This time slot is no longer available");
        }
    }
}