using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Providers.Queries
{
    public record GetProvidersQuery : IRequest<List<ProviderDto>>
    {
        public bool IncludeInactive { get; init; } = false;
    }

    public record ProviderDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Specialty { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<WorkingHoursDto> WorkingHours { get; init; } = new();
    }

    public record WorkingHoursDto
    {
        public DayOfWeek DayOfWeek { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }

    public class GetProvidersHandler : IRequestHandler<GetProvidersQuery, List<ProviderDto>>
    {
        private readonly IAppDbContext _context;

        public GetProvidersHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProviderDto>> Handle(GetProvidersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.ServiceProviders
                .Include(p => p.WorkingHours.Where(wh => wh.IsActive))
                .AsQueryable();

            if (!request.IncludeInactive)
                query = query.Where(p => p.IsActive);

            var providers = await query
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return providers.Select(p => new ProviderDto
            {
                Id = p.Id,
                Name = p.Name,
                Email = p.Email,
                Specialty = p.Specialty,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                WorkingHours = p.WorkingHours.Select(wh => new WorkingHoursDto
                {
                    DayOfWeek = wh.DayOfWeek,
                    StartTime = wh.StartTime,
                    EndTime = wh.EndTime
                }).ToList()
            }).ToList();
        }
    }
}
