using Application.Common.Interfaces;
using Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Providers.Commands
{
    public record UpdateProviderCommand : IRequest<Unit>
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Specialty { get; init; } = string.Empty;
    }

    public class UpdateProviderValidator : AbstractValidator<UpdateProviderCommand>
    {
        private readonly IAppDbContext _context;

        public UpdateProviderValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Id)
                .NotEmpty()
                .MustAsync(ProviderExists)
                .WithMessage("Provider not found");

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(200)
                .MustAsync(BeUniqueEmail)
                .WithMessage("Email must be unique");

            RuleFor(x => x.Specialty)
                .NotEmpty()
                .MaximumLength(100);
        }

        private async Task<bool> ProviderExists(Guid providerId, CancellationToken cancellationToken)
        {
            return await _context.ServiceProviders
                .AnyAsync(p => p.Id == providerId && p.IsActive, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(UpdateProviderCommand command, string email, CancellationToken cancellationToken)
        {
            return !await _context.ServiceProviders
                .AnyAsync(p => p.Email == email.ToLowerInvariant() && p.Id != command.Id, cancellationToken);
        }
    }

    public class UpdateProviderHandler : IRequestHandler<UpdateProviderCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<UpdateProviderHandler> _logger;

        public UpdateProviderHandler(
            IAppDbContext context,
            IDateTimeService dateTimeService,
            ILogger<UpdateProviderHandler> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
        {
            var provider = await _context.ServiceProviders
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

            if (provider == null)
                throw new BusinessRuleException("Provider not found");

            provider.UpdateDetails(request.Name, request.Email, request.Specialty, _dateTimeService.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated provider {ProviderId}", request.Id);
            return Unit.Value;
        }
    }
}