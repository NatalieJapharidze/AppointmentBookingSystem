using Application.Common.Interfaces;
using Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.Commands
{
    public record CancelAppointmentCommand : IRequest
    {
        public Guid AppointmentId { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class CancelAppointmentValidator : AbstractValidator<CancelAppointmentCommand>
    {
        private readonly IAppDbContext _context;

        public CancelAppointmentValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Appointment ID is required")
                .MustAsync(AppointmentExists)
                .WithMessage("Appointment not found");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Cancellation reason is required")
                .MaximumLength(500)
                .WithMessage("Cancellation reason cannot exceed 500 characters");

            RuleFor(x => x)
                .MustAsync(AppointmentCanBeCancelled)
                .WithMessage("This appointment cannot be cancelled");
        }

        private async Task<bool> AppointmentExists(Guid appointmentId, CancellationToken cancellationToken)
        {
            return await _context.Appointments
                .AnyAsync(a => a.Id == appointmentId, cancellationToken);
        }

        private async Task<bool> AppointmentCanBeCancelled(CancelAppointmentCommand command, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken);

            if (appointment == null) return false;

            return appointment.Status == Domain.Enums.AppointmentStatus.Scheduled &&
                   appointment.AppointmentDateTime > DateTime.UtcNow;
        }
    }

    public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<CancelAppointmentCommandHandler> _logger;

        public CancelAppointmentCommandHandler(
            IAppDbContext context,
            IEmailService emailService,
            IDateTimeService dateTimeService,
            ILogger<CancelAppointmentCommandHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cancelling appointment {AppointmentId}", request.AppointmentId);

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

                if (appointment == null)
                    throw new BusinessRuleException("Appointment not found");

                var currentUtcTime = _dateTimeService.UtcNow;

                appointment.Cancel(request.Reason, currentUtcTime);

                await _context.SaveChangesAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendCancellationEmailAsync(appointment, request.Reason, cancellationToken);
                        _logger.LogInformation("Cancellation email sent for appointment {AppointmentId}", appointment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send cancellation email for appointment {AppointmentId}", appointment.Id);
                    }
                }, cancellationToken);

                _logger.LogInformation("Successfully cancelled appointment {AppointmentId}", request.AppointmentId);
            }
            catch (BusinessRuleException)
            {
                _logger.LogWarning("Business rule violation when cancelling appointment {AppointmentId}", request.AppointmentId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", request.AppointmentId);
                throw new InvalidOperationException("Failed to cancel appointment. Please try again.", ex);
            }
        }
    }
}