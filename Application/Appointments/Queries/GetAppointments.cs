using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Appointments.Queries
{
    public record GetAppointmentsQuery : IRequest<List<AppointmentDto>>
    {
        public Guid? ProviderId { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public string? CustomerEmail { get; init; }
        public Domain.Enums.AppointmentStatus? Status { get; init; }
    }
    public record AppointmentDto
    {
        public Guid Id { get; init; }
        public Guid ProviderId { get; init; }
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
    }
    public class GetAppointmentsQueryHandler : IRequestHandler<GetAppointmentsQuery, List<AppointmentDto>>
    {
        private readonly IAppDbContext _context;

        public GetAppointmentsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AppointmentDto>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Appointments.AsQueryable();

            if (request.ProviderId.HasValue)
                query = query.Where(a => a.ProviderId == request.ProviderId.Value);

            if (request.FromDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
                query = query.Where(a => a.AppointmentDate <= request.ToDate.Value.Date);

            if (!string.IsNullOrEmpty(request.CustomerEmail))
                query = query.Where(a => a.CustomerEmail == request.CustomerEmail);

            if (request.Status.HasValue)
                query = query.Where(a => a.Status == request.Status.Value);

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync(cancellationToken);

            return appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                ProviderId = a.ProviderId,
                CustomerName = a.CustomerName,
                CustomerEmail = a.CustomerEmail,
                CustomerPhone = a.CustomerPhone,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status,
                CancellationReason = a.CancellationReason,
                IsRecurring = a.IsRecurring,
                CreatedAt = a.CreatedAt
            }).ToList();
        }
    }
}
