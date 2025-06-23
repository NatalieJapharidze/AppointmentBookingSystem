using Application.Providers.Commands;

namespace WebApi.Models
{
    public record UpdateProviderRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Specialty { get; init; } = string.Empty;
    }
}
