using Application.Common.Interfaces;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Providers.Commands
{
    public record AddWorkingHoursCommand : IRequest<Unit>
    {
        public Guid ProviderId { get; init; }
        public DayOfWeek DayOfWeek { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }

    public class AddWorkingHoursValidator : AbstractValidator<AddWorkingHoursCommand>
    {
        private readonly IAppDbContext _context;

        public AddWorkingHoursValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .MustAsync(ProviderExists)
                .WithMessage("Provider not found");

            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime)
                .WithMessage("Start time must be before end time");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time");
        }

        private async Task<bool> ProviderExists(Guid providerId, CancellationToken cancellationToken)
        {
            return await _context.ServiceProviders
                .AnyAsync(p => p.Id == providerId && p.IsActive, cancellationToken);
        }
    }

    public class AddWorkingHoursHandler : IRequestHandler<AddWorkingHoursCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<AddWorkingHoursHandler> _logger;

        public AddWorkingHoursHandler(
            IAppDbContext context,
            IDateTimeService dateTimeService,
            ILogger<AddWorkingHoursHandler> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Unit> Handle(AddWorkingHoursCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding working hours for provider {ProviderId} on {DayOfWeek} from {StartTime} to {EndTime}",
                request.ProviderId, request.DayOfWeek, request.StartTime, request.EndTime);

            try
            {
                var currentUtcTime = _dateTimeService.UtcNow;

                var existingWorkingHours = await _context.WorkingHours
                    .Where(wh => wh.ProviderId == request.ProviderId &&
                                wh.DayOfWeek == request.DayOfWeek &&
                                wh.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var existing in existingWorkingHours)
                {
                    existing.Deactivate(currentUtcTime);
                }

                var workingHours = WorkingHours.Create(
                    request.ProviderId,
                    request.DayOfWeek,
                    request.StartTime,
                    request.EndTime,
                    currentUtcTime);

                _context.WorkingHours.Add(workingHours);

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added working hours for provider {ProviderId} on {DayOfWeek}",
                    request.ProviderId, request.DayOfWeek);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding working hours for provider {ProviderId}. Exception: {Exception}",
                    request.ProviderId, ex.ToString());
                throw;
            }
        }
    }
}