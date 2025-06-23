using Domain.Common;

namespace Domain.Entities
{
    public class WorkingHours : BaseEntity
    {
        public Guid ProviderId { get; internal set; }
        public DayOfWeek DayOfWeek { get; internal set; }
        public TimeOnly StartTime { get; internal set; }
        public TimeOnly EndTime { get; internal set; }
        public bool IsActive { get; internal set; } = true;

        private WorkingHours() : base() { }

        public static WorkingHours Create(
            Guid providerId,
            DayOfWeek dayOfWeek,
            TimeOnly startTime,
            TimeOnly endTime,
            DateTime currentUtcTime)
        {
            if (endTime <= startTime)
                throw new BusinessRuleException("End time must be after start time");

            var workingHours = new WorkingHours
            {
                ProviderId = providerId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                IsActive = true
            };

            workingHours.SetCreationTimestamp(currentUtcTime);
            return workingHours;
        }

        public void Deactivate(DateTime currentUtcTime)
        {
            IsActive = false;
            UpdateTimestamp(currentUtcTime);
        }

        public bool IsTimeWithinWorkingHours(TimeOnly time) =>
            IsActive && time >= StartTime && time <= EndTime;

        public bool IsTimeSlotWithinWorkingHours(TimeOnly startTime, TimeOnly endTime) =>
            IsActive && startTime >= StartTime && endTime <= EndTime;
    }
}