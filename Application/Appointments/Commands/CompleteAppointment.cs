using Application.Common.Interfaces;
using Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.Commands
{
    public record CompleteAppointmentCommand : IRequest<Unit>
    {
        public Guid AppointmentId { get; init; }
    }

    public class CompleteAppointmentValidator : AbstractValidator<CompleteAppointmentCommand>
    {
        private readonly IAppDbContext _context;

        public CompleteAppointmentValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .MustAsync(AppointmentExistsAndCanBeCompleted)
                .WithMessage("Appointment not found or cannot be completed");
        }

        private async Task<bool> AppointmentExistsAndCanBeCompleted(Guid appointmentId, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            return appointment != null && appointment.Status == Domain.Enums.AppointmentStatus.Scheduled;
        }
    }

    public class CompleteAppointmentHandler : IRequestHandler<CompleteAppointmentCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<CompleteAppointmentHandler> _logger;

        public CompleteAppointmentHandler(
            IAppDbContext context,
            IDateTimeService dateTimeService,
            ILogger<CompleteAppointmentHandler> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Unit> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

            if (appointment == null)
                throw new BusinessRuleException("Appointment not found");

            appointment.MarkAsCompleted(_dateTimeService.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed appointment {AppointmentId}", request.AppointmentId);
            return Unit.Value;
        }
    }
}