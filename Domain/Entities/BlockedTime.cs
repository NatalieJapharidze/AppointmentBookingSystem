using Domain.Common;

namespace Domain.Entities
{
    public class BlockedTime : BaseEntity
    {
        public Guid ProviderId { get; internal set; }
        public DateTime StartDateTime { get; internal set; }
        public DateTime EndDateTime { get; internal set; }
        public string Reason { get; internal set; } = string.Empty;

        private BlockedTime() : base() { }

        public static BlockedTime Create(
            Guid providerId,
            DateTime startDateTime,
            DateTime endDateTime,
            string reason,
            DateTime currentUtcTime)
        {
            if (endDateTime <= startDateTime)
                throw new BusinessRuleException("End time must be after start time");

            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("Reason is required");

            var blockedTime = new BlockedTime
            {
                ProviderId = providerId,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                Reason = reason.Trim()
            };
            blockedTime.SetCreationTimestamp(currentUtcTime);

            return blockedTime;
        }

        public bool ConflictsWith(DateTime appointmentDateTime, int durationMinutes)
        {
            var appointmentEnd = appointmentDateTime.AddMinutes(durationMinutes);
            return appointmentDateTime < EndDateTime && appointmentEnd > StartDateTime;
        }
    }
}