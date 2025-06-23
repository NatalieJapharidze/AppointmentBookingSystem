using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Providers.Commands
{
    public record CreateProviderCommand : IRequest<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Specialty { get; init; } = string.Empty;
    }

    public class CreateProviderValidator : AbstractValidator<CreateProviderCommand>
    {
        private readonly IAppDbContext _context;

        public CreateProviderValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Name is required and cannot exceed 200 characters");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(200)
                .MustAsync(BeUniqueEmail)
                .WithMessage("Email must be unique");

            RuleFor(x => x.Specialty)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Specialty is required and cannot exceed 100 characters");
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.ServiceProviders
                .AnyAsync(p => p.Email == email.ToLowerInvariant(), cancellationToken);
        }
    }

    public class CreateProviderHandler : IRequestHandler<CreateProviderCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<CreateProviderHandler> _logger;

        public CreateProviderHandler(
            IAppDbContext context,
            IDateTimeService dateTimeService,
            ILogger<CreateProviderHandler> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
        {
            var provider = ServiceProvider.Create(
                request.Name,
                request.Email,
                request.Specialty,
                _dateTimeService.UtcNow);

            _context.ServiceProviders.Add(provider);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created provider {ProviderId} with name {Name}", provider.Id, provider.Name);
            return provider.Id;
        }
    }
}