using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Providers.Queries
{
    public record GetProviderQuery : IRequest<ProviderDetailDto?>
    {
        public Guid Id { get; init; }
    }

    public record ProviderDetailDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Specialty { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<WorkingHoursDto> WorkingHours { get; init; } = new();
        public List<BlockedTimeDto> BlockedTimes { get; init; } = new();
    }

    public record BlockedTimeDto
    {
        public Guid Id { get; init; }
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class GetProviderHandler : IRequestHandler<GetProviderQuery, ProviderDetailDto?>
    {
        private readonly IAppDbContext _context;

        public GetProviderHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<ProviderDetailDto?> Handle(GetProviderQuery request, CancellationToken cancellationToken)
        {
            var provider = await _context.ServiceProviders
                .Include(p => p.WorkingHours.Where(wh => wh.IsActive))
                .Include(p => p.BlockedTimes)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (provider == null)
                return null;

            return new ProviderDetailDto
            {
                Id = provider.Id,
                Name = provider.Name,
                Email = provider.Email,
                Specialty = provider.Specialty,
                IsActive = provider.IsActive,
                CreatedAt = provider.CreatedAt,
                WorkingHours = provider.WorkingHours.Select(wh => new WorkingHoursDto
                {
                    DayOfWeek = wh.DayOfWeek,
                    StartTime = wh.StartTime,
                    EndTime = wh.EndTime
                }).ToList(),
                BlockedTimes = provider.BlockedTimes.Select(bt => new BlockedTimeDto
                {
                    Id = bt.Id,
                    StartDateTime = bt.StartDateTime,
                    EndDateTime = bt.EndDateTime,
                    Reason = bt.Reason
                }).ToList()
            };
        }
    }
}
