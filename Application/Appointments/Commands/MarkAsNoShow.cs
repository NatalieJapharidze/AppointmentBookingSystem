using Application.Common.Interfaces;
using Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.Commands
{
    public record MarkAsNoShowCommand : IRequest<Unit>
    {
        public Guid AppointmentId { get; init; }
    }

    public class MarkAsNoShowValidator : AbstractValidator<MarkAsNoShowCommand>
    {
        private readonly IAppDbContext _context;

        public MarkAsNoShowValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .MustAsync(AppointmentExistsAndCanBeMarkedAsNoShow)
                .WithMessage("Appointment not found or cannot be marked as no-show");
        }

        private async Task<bool> AppointmentExistsAndCanBeMarkedAsNoShow(Guid appointmentId, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            return appointment != null && appointment.Status == Domain.Enums.AppointmentStatus.Scheduled;
        }
    }

    public class MarkAsNoShowHandler : IRequestHandler<MarkAsNoShowCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<MarkAsNoShowHandler> _logger;

        public MarkAsNoShowHandler(
            IAppDbContext context,
            IDateTimeService dateTimeService,
            ILogger<MarkAsNoShowHandler> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Unit> Handle(MarkAsNoShowCommand request, CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

            if (appointment == null)
                throw new BusinessRuleException("Appointment not found");

            appointment.MarkAsNoShow(_dateTimeService.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked appointment {AppointmentId} as no-show", request.AppointmentId);
            return Unit.Value;
        }
    }
}