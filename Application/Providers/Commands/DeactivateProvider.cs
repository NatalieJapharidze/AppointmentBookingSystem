using Application.Common.Interfaces;
using Domain.Common;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Providers.Commands
{
    public record DeactivateProviderCommand : IRequest<Unit>
    {
        public Guid Id { get; init; }
    }

    public class DeactivateProviderValidator : AbstractValidator<DeactivateProviderCommand>
    {
        private readonly IAppDbContext _context;

        public DeactivateProviderValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Id)
                .NotEmpty()
                .MustAsync(ProviderExists)
                .WithMessage("Provider not found");
        }

        private async Task<bool> ProviderExists(Guid providerId, CancellationToken cancellationToken)
        {
            return await _context.ServiceProviders
                .AnyAsync(p => p.Id == providerId && p.IsActive, cancellationToken);
        }
    }

    public class DeactivateProviderHandler : IRequestHandler<DeactivateProviderCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<DeactivateProviderHandler> _logger;

        public DeactivateProviderHandler(
            IAppDbContext context,
            IDateTimeService dateTimeService,
            ILogger<DeactivateProviderHandler> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeactivateProviderCommand request, CancellationToken cancellationToken)
        {
            var provider = await _context.ServiceProviders
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

            if (provider == null)
                throw new BusinessRuleException("Provider not found");

            provider.Deactivate(_dateTimeService.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deactivated provider {ProviderId}", request.Id);
            return Unit.Value;
        }
    }
}