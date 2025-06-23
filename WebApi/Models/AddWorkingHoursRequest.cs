namespace WebApi.Models
{
    public record AddWorkingHoursRequest
    {
        public DayOfWeek DayOfWeek { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }
}
