namespace WebApi.Models
{
    public record CancelAppointmentRequest
    {
        public string Reason { get; init; } = string.Empty;
    }
}
