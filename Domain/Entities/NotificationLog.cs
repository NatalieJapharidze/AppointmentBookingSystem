using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class NotificationLog : BaseEntity
    {
        public Guid AppointmentId { get; internal set; }
        public NotificationType Type { get; internal set; }
        public DateTime? SentAt { get; internal set; }
        public NotificationStatus Status { get; internal set; } = NotificationStatus.Pending;
        public string? ErrorMessage { get; internal set; }
        public int RetryCount { get; internal set; } = 0;

        private NotificationLog() : base() { }

        public static NotificationLog Create(
            Guid appointmentId,
            NotificationType type,
            DateTime currentUtcTime)
        {
            var notification = new NotificationLog
            {
                AppointmentId = appointmentId,
                Type = type,
                Status = NotificationStatus.Pending,
                RetryCount = 0
            };
            notification.SetCreationTimestamp(currentUtcTime);

            return notification;
        }

        public void MarkAsSent(DateTime sentAt)
        {
            Status = NotificationStatus.Sent;
            SentAt = sentAt;
            UpdateTimestamp(sentAt);
        }

        public void MarkAsFailed(string errorMessage, DateTime currentUtcTime)
        {
            Status = NotificationStatus.Failed;
            ErrorMessage = errorMessage;
            RetryCount++;
            UpdateTimestamp(currentUtcTime);
        }
    }
}