using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.Queries
{
    public record GetAppointmentQuery : IRequest<AppointmentDetailDto?>
    {
        public Guid Id { get; init; }
    }

    public record AppointmentDetailDto
    {
        public Guid Id { get; init; }
        public Guid ProviderId { get; init; }
        public string ProviderName { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerEmail { get; init; } = string.Empty;
        public string CustomerPhone { get; init; } = string.Empty;
        public DateTime AppointmentDate { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
        public AppointmentStatus Status { get; init; }
        public string? CancellationReason { get; init; }
        public bool IsRecurring { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public class GetAppointmentHandler : IRequestHandler<GetAppointmentQuery, AppointmentDetailDto?>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<GetAppointmentHandler> _logger;

        public GetAppointmentHandler(IAppDbContext context, ILogger<GetAppointmentHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AppointmentDetailDto?> Handle(GetAppointmentQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting appointment {AppointmentId}", request.Id);

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment {AppointmentId} not found", request.Id);
                    return null;
                }

                var provider = await _context.ServiceProviders
                    .FirstOrDefaultAsync(p => p.Id == appointment.ProviderId, cancellationToken);

                return new AppointmentDetailDto
                {
                    Id = appointment.Id,
                    ProviderId = appointment.ProviderId,
                    ProviderName = provider?.Name ?? "Unknown Provider",
                    CustomerName = appointment.CustomerName,
                    CustomerEmail = appointment.CustomerEmail,
                    CustomerPhone = appointment.CustomerPhone,
                    AppointmentDate = appointment.AppointmentDate,
                    StartTime = appointment.StartTime,
                    EndTime = appointment.EndTime,
                    Status = appointment.Status,
                    CancellationReason = appointment.CancellationReason,
                    IsRecurring = appointment.IsRecurring,
                    CreatedAt = appointment.CreatedAt,
                    UpdatedAt = appointment.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment {AppointmentId}", request.Id);
                throw;
            }
        }
    }
}