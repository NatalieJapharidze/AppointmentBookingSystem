using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Providers.Commands
{
    public record BlockTimeCommand : IRequest<Unit>
    {
        public Guid ProviderId { get; init; }
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class BlockTimeValidator : AbstractValidator<BlockTimeCommand>
    {
        private readonly IAppDbContext _context;

        public BlockTimeValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .MustAsync(ProviderExists)
                .WithMessage("Provider not found");

            RuleFor(x => x.StartDateTime)
                .LessThan(x => x.EndDateTime)
                .WithMessage("Start time must be before end time");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .MaximumLength(500)
                .WithMessage("Reason is required and cannot exceed 500 characters");
        }

        private async Task<bool> ProviderExists(Guid providerId, CancellationToken cancellationToken)
        {
            return await _context.ServiceProviders
                .AnyAsync(p => p.Id == providerId && p.IsActive, cancellationToken);
        }
    }

    public class BlockTimeHandler : IRequestHandler<BlockTimeCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<BlockTimeHandler> _logger;

        public BlockTimeHandler(
            IAppDbContext context,
            IDateTimeService dateTimeService,
            ILogger<BlockTimeHandler> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Unit> Handle(BlockTimeCommand request, CancellationToken cancellationToken)
        {
            var provider = await _context.ServiceProviders
                .Include(p => p.BlockedTimes)
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId && p.IsActive, cancellationToken);

            if (provider == null)
                throw new BusinessRuleException("Provider not found");

            provider.BlockTime(request.StartDateTime, request.EndDateTime, request.Reason, _dateTimeService.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Blocked time for provider {ProviderId} from {Start} to {End}",
                request.ProviderId, request.StartDateTime, request.EndDateTime);
            return Unit.Value;
        }
    }
}
