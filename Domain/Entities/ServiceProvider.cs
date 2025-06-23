using Domain.Common;

namespace Domain.Entities
{
    public class ServiceProvider : AggregateRoot
    {
        public string Name { get; internal set; } = string.Empty;
        public string Email { get; internal set; } = string.Empty;
        public string Specialty { get; internal set; } = string.Empty;
        public bool IsActive { get; internal set; } = true;

        private readonly List<WorkingHours> _workingHours = new();
        public IReadOnlyCollection<WorkingHours> WorkingHours => _workingHours.AsReadOnly();

        private readonly List<BlockedTime> _blockedTimes = new();
        public IReadOnlyCollection<BlockedTime> BlockedTimes => _blockedTimes.AsReadOnly();

        private ServiceProvider() : base()
        {
            _workingHours = new List<WorkingHours>();
            _blockedTimes = new List<BlockedTime>();
        }

        public static ServiceProvider Create(
            string name,
            string email,
            string specialty,
            DateTime currentUtcTime)
        {
            ValidateProviderInfo(name, email, specialty);

            var provider = new ServiceProvider
            {
                Name = name.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                Specialty = specialty.Trim(),
                IsActive = true
            };
            provider.SetCreationTimestamp(currentUtcTime);

            return provider;
        }

        private static void ValidateProviderInfo(string name, string email, string specialty)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleException("Provider name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new BusinessRuleException("Provider email is required");

            if (string.IsNullOrWhiteSpace(specialty))
                throw new BusinessRuleException("Provider specialty is required");

            if (!email.Contains('@'))
                throw new BusinessRuleException("Invalid email format");
        }

        public void AddWorkingHours(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateTime currentUtcTime)
        {
            if (endTime <= startTime)
                throw new BusinessRuleException("End time must be after start time");

            var existingHours = _workingHours.FirstOrDefault(wh => wh.DayOfWeek == dayOfWeek && wh.IsActive);
            existingHours?.Deactivate(currentUtcTime);

            var workingHours = Entities.WorkingHours.Create(Id, dayOfWeek, startTime, endTime, currentUtcTime);
            _workingHours.Add(workingHours);
            UpdateTimestamp(currentUtcTime);
        }

        public void BlockTime(DateTime startDateTime, DateTime endDateTime, string reason, DateTime currentUtcTime)
        {
            if (endDateTime <= startDateTime)
                throw new BusinessRuleException("End time must be after start time");

            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("Block reason is required");

            var blockedTime = BlockedTime.Create(Id, startDateTime, endDateTime, reason, currentUtcTime);
            _blockedTimes.Add(blockedTime);
            UpdateTimestamp(currentUtcTime);
        }

        public bool IsAvailableAt(DateTime dateTime, int durationMinutes)
        {
            if (!IsActive)
                return false;

            var dayOfWeek = dateTime.DayOfWeek;
            var timeOfDay = TimeOnly.FromDateTime(dateTime);
            var endTime = timeOfDay.AddMinutes(durationMinutes);

            var workingHours = _workingHours
                .FirstOrDefault(wh => wh.DayOfWeek == dayOfWeek && wh.IsActive);

            if (workingHours == null || !workingHours.IsTimeSlotWithinWorkingHours(timeOfDay, endTime))
                return false;

            return !_blockedTimes.Any(bt => bt.ConflictsWith(dateTime, durationMinutes));
        }

        public WorkingHours? GetWorkingHoursForDay(DayOfWeek dayOfWeek)
        {
            return _workingHours.FirstOrDefault(wh => wh.DayOfWeek == dayOfWeek && wh.IsActive);
        }

        public void UpdateDetails(string name, string email, string specialty, DateTime currentUtcTime)
        {
            ValidateProviderInfo(name, email, specialty);

            Name = name.Trim();
            Email = email.Trim().ToLowerInvariant();
            Specialty = specialty.Trim();
            UpdateTimestamp(currentUtcTime);
        }

        public void Deactivate(DateTime currentUtcTime)
        {
            if (!IsActive)
                throw new BusinessRuleException("Provider is already deactivated");

            IsActive = false;
            UpdateTimestamp(currentUtcTime);
        }

        public void Activate(DateTime currentUtcTime)
        {
            if (IsActive)
                throw new BusinessRuleException("Provider is already active");

            IsActive = true;
            UpdateTimestamp(currentUtcTime);
        }
    }
}