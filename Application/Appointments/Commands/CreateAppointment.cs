using Application.Common.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.Commands
{
    public record CreateAppointmentCommand : IRequest<CreateAppointmentResult>
    {
        public Guid ProviderId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerEmail { get; init; } = string.Empty;
        public string CustomerPhone { get; init; } = string.Empty;
        public DateTime AppointmentDate { get; init; }
        public TimeOnly StartTime { get; init; }
        public int DurationMinutes { get; init; }
        public RecurrenceRule? RecurrenceRule { get; init; }
    }

    public record CreateAppointmentResult
    {
        public Guid AppointmentId { get; init; }
        public List<Guid> RecurringAppointmentIds { get; init; } = new();
        public int TotalAppointmentsCreated { get; init; }
    }

    public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentCommand>
    {
        private readonly IAppDbContext _context;

        public CreateAppointmentValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .MustAsync(ProviderExistsAndIsActive)
                .WithMessage("Provider not found or inactive");

            RuleFor(x => x.CustomerName)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.CustomerEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(200);

            RuleFor(x => x.CustomerPhone)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.DurationMinutes)
                .Must(d => new[] { 15, 30, 45, 60 }.Contains(d))
                .WithMessage("Duration must be 15, 30, 45, or 60 minutes");

            RuleFor(x => x)
                .MustAsync(BeWithinProviderWorkingHours)
                .WithMessage("Selected time is outside provider's working hours")
                .MustAsync(NotConflictWithExistingAppointments)
                .WithMessage("This time slot conflicts with an existing appointment");
        }

        private async Task<bool> ProviderExistsAndIsActive(Guid providerId, CancellationToken cancellationToken)
        {
            return await _context.ServiceProviders
                .Where(p => p.Id == providerId && p.IsActive)
                .AnyAsync(cancellationToken);
        }

        private async Task<bool> BeWithinProviderWorkingHours(CreateAppointmentCommand command, CancellationToken cancellationToken)
        {
            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(wh => wh.ProviderId == command.ProviderId &&
                                         wh.DayOfWeek == command.AppointmentDate.DayOfWeek &&
                                         wh.IsActive, cancellationToken);

            if (workingHours == null) return false;

            var endTime = command.StartTime.AddMinutes(command.DurationMinutes);
            return command.StartTime >= workingHours.StartTime && endTime <= workingHours.EndTime;
        }

        private async Task<bool> NotConflictWithExistingAppointments(CreateAppointmentCommand command, CancellationToken cancellationToken)
        {
            var endTime = command.StartTime.AddMinutes(command.DurationMinutes);

            return !await _context.Appointments
                .Where(a => a.ProviderId == command.ProviderId
                        && a.AppointmentDate.Date == command.AppointmentDate.Date
                        && a.Status == Domain.Enums.AppointmentStatus.Scheduled)
                .AnyAsync(a => a.StartTime < endTime && a.EndTime > command.StartTime, cancellationToken);
        }
    }

    public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, CreateAppointmentResult>
    {
        private readonly IAppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<CreateAppointmentCommandHandler> _logger;

        public CreateAppointmentCommandHandler(
            IAppDbContext context,
            IEmailService emailService,
            IDateTimeService dateTimeService,
            ILogger<CreateAppointmentCommandHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<CreateAppointmentResult> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            var timeSlot = TimeSlot.FromStartAndDuration(request.StartTime, request.DurationMinutes);
            var currentUtcTime = _dateTimeService.UtcNow;

            var appointment = Appointment.Create(
                request.ProviderId,
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerPhone,
                request.AppointmentDate,
                timeSlot,
                currentUtcTime,
                request.RecurrenceRule);

            _context.Appointments.Add(appointment);

            var result = new CreateAppointmentResult
            {
                AppointmentId = appointment.Id,
                TotalAppointmentsCreated = 1
            };

            if (request.RecurrenceRule != null)
            {
                var recurringIds = await CreateRecurringAppointments(appointment, request.RecurrenceRule, currentUtcTime, cancellationToken);
                result = result with
                {
                    RecurringAppointmentIds = recurringIds,
                    TotalAppointmentsCreated = 1 + recurringIds.Count
                };
            }

            await _context.SaveChangesAsync(cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendConfirmationEmailAsync(appointment, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email for appointment {AppointmentId}", appointment.Id);
                }
            }, cancellationToken);

            return result;
        }

        private async Task<List<Guid>> CreateRecurringAppointments(
            Appointment parentAppointment,
            RecurrenceRule recurrenceRule,
            DateTime currentUtcTime,
            CancellationToken cancellationToken)
        {
            var recurringIds = new List<Guid>();
            var currentDate = parentAppointment.AppointmentDate;
            var occurrenceCount = 1;
            const int maxOccurrences = 52;

            while (occurrenceCount < maxOccurrences)
            {
                try
                {
                    currentDate = recurrenceRule.GetNextOccurrence(currentDate);

                    if (currentDate > currentUtcTime.Date.AddMonths(3))
                        break;

                    var hasConflict = await _context.Appointments
                        .Where(a => a.ProviderId == parentAppointment.ProviderId
                                && a.AppointmentDate.Date == currentDate.Date
                                && a.Status == Domain.Enums.AppointmentStatus.Scheduled)
                        .AnyAsync(a => a.StartTime < parentAppointment.EndTime && a.EndTime > parentAppointment.StartTime,
                                cancellationToken);

                    if (!hasConflict)
                    {
                        var recurringAppointment = Appointment.Create(
                            parentAppointment.ProviderId,
                            parentAppointment.CustomerName,
                            parentAppointment.CustomerEmail,
                            parentAppointment.CustomerPhone,
                            currentDate,
                            parentAppointment.TimeSlot,
                            currentUtcTime,
                            parentAppointmentId: parentAppointment.Id);

                        _context.Appointments.Add(recurringAppointment);
                        recurringIds.Add(recurringAppointment.Id);
                    }

                    occurrenceCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to create recurring appointment: {Error}", ex.Message);
                    break;
                }
            }

            return recurringIds;
        }
    }
}