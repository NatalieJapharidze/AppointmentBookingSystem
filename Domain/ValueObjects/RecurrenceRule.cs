using Domain.Enums;

namespace Domain.ValueObjects
{
    public record RecurrenceRule
    {
        public RecurrenceType Type { get; }
        public int Interval { get; }
        public DateTime? EndDate { get; }

        public RecurrenceRule(RecurrenceType type, int interval = 1, DateTime? endDate = null)
        {
            if (type == RecurrenceType.None)
                throw new ArgumentException("Use null instead of RecurrenceType.None");

            if (interval < 1)
                throw new ArgumentException("Interval must be at least 1");

            Type = type;
            Interval = interval;
            EndDate = endDate;
        }

        public static RecurrenceRule Weekly(int interval = 1, DateTime? endDate = null) =>
            new(RecurrenceType.Weekly, interval, endDate);

        public static RecurrenceRule Monthly(int interval = 1, DateTime? endDate = null) =>
            new(RecurrenceType.Monthly, interval, endDate);

        public DateTime GetNextOccurrence(DateTime currentDate)
        {
            return Type switch
            {
                RecurrenceType.Weekly => currentDate.AddDays(7 * Interval),
                RecurrenceType.Monthly => currentDate.AddMonths(Interval),
                _ => throw new InvalidOperationException($"Unsupported recurrence type: {Type}")
            };
        }
    }
}
