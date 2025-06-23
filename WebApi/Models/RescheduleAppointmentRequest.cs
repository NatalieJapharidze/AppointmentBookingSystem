namespace WebApi.Models
{
    public record RescheduleAppointmentRequest
    {
        public DateTime NewAppointmentDate { get; init; }
        public TimeOnly NewStartTime { get; init; }
        public int DurationMinutes { get; init; }
    }
}
