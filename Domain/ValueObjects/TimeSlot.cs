using Domain.Common;

namespace Domain.ValueObjects
{
    public record TimeSlot
    {
        public TimeOnly StartTime { get; }
        public TimeOnly EndTime { get; }
        public int DurationMinutes { get; }

        public TimeSlot(TimeOnly startTime, TimeOnly endTime)
        {
            if (endTime <= startTime)
                throw new ArgumentException("End time must be after start time");

            var duration = (int)(endTime - startTime).TotalMinutes;
            var validDurations = new[] { 15, 30, 45, 60 };

            if (!validDurations.Contains(duration))
                throw new BusinessRuleException("Duration must be 15, 30, 45, or 60 minutes");

            StartTime = startTime;
            EndTime = endTime;
            DurationMinutes = duration;
        }

        public bool Overlaps(TimeSlot other) =>
            StartTime < other.EndTime && EndTime > other.StartTime;

        public static TimeSlot FromStartAndDuration(TimeOnly startTime, int durationMinutes)
        {
            var endTime = startTime.AddMinutes(durationMinutes);
            return new TimeSlot(startTime, endTime);
        }

        public override string ToString() =>
            $"{StartTime:HH:mm} - {EndTime:HH:mm}";
    }
}