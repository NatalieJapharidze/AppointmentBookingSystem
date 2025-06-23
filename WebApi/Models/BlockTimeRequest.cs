namespace WebApi.Models
{
    public record BlockTimeRequest
    {
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}
