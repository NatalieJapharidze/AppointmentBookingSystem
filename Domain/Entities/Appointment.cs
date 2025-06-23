using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public class Appointment : AggregateRoot
    {
        public Guid ProviderId { get; internal set; }
        public string CustomerName { get; internal set; } = string.Empty;
        public string CustomerEmail { get; internal set; } = string.Empty;
        public string CustomerPhone { get; internal set; } = string.Empty;
        public DateTime AppointmentDate { get; internal set; }
        public TimeOnly StartTime { get; internal set; }
        public TimeOnly EndTime { get; internal set; }
        public AppointmentStatus Status { get; internal set; } = AppointmentStatus.Scheduled;
        public string? CancellationReason { get; internal set; }
        public bool IsRecurring { get; internal set; }
        public RecurrenceRule? RecurrenceRule { get; internal set; }
        public Guid? ParentAppointmentId { get; internal set; }

        public TimeSlot TimeSlot => new(StartTime, EndTime);
        public int DurationMinutes => TimeSlot.DurationMinutes;
        public DateTime AppointmentDateTime => AppointmentDate.Date.Add(StartTime.ToTimeSpan());

        private Appointment() : base() { }

        public static Appointment Create(
            Guid providerId,
            string customerName,
            string customerEmail,
            string customerPhone,
            DateTime appointmentDate,
            TimeSlot timeSlot,
            DateTime currentUtcTime,
            RecurrenceRule? recurrenceRule = null,
            Guid? parentAppointmentId = null)
        {
            ValidateBusinessRules(appointmentDate, timeSlot, currentUtcTime);
            ValidateCustomerInfo(customerName, customerEmail, customerPhone);

            var appointment = new Appointment
            {
                ProviderId = providerId,
                CustomerName = customerName.Trim(),
                CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
                CustomerPhone = customerPhone.Trim(),
                AppointmentDate = appointmentDate.Date,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                RecurrenceRule = recurrenceRule,
                ParentAppointmentId = parentAppointmentId,
                IsRecurring = recurrenceRule != null,
                Status = AppointmentStatus.Scheduled
            };

            appointment.SetCreationTimestamp(currentUtcTime);
            return appointment;
        }

        private static void ValidateBusinessRules(DateTime appointmentDate, TimeSlot timeSlot, DateTime currentUtcTime)
        {
            var appointmentDateTime = appointmentDate.Date.Add(timeSlot.StartTime.ToTimeSpan());

            if (appointmentDateTime <= currentUtcTime.AddHours(24))
                throw new BusinessRuleException("Appointments must be booked at least 24 hours in advance");

            if (appointmentDate.Date > currentUtcTime.Date.AddMonths(3))
                throw new BusinessRuleException("Cannot book more than 3 months in advance");

            if (appointmentDate.Date < currentUtcTime.Date)
                throw new BusinessRuleException("Cannot book appointments in the past");
        }

        private static void ValidateCustomerInfo(string customerName, string customerEmail, string customerPhone)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new BusinessRuleException("Customer name is required");

            if (string.IsNullOrWhiteSpace(customerEmail))
                throw new BusinessRuleException("Customer email is required");

            if (string.IsNullOrWhiteSpace(customerPhone))
                throw new BusinessRuleException("Customer phone is required");

            if (!customerEmail.Contains('@'))
                throw new BusinessRuleException("Invalid email format");
        }

        public void Cancel(string reason, DateTime currentUtcTime)
        {
            if (Status != AppointmentStatus.Scheduled)
                throw new BusinessRuleException("Can only cancel scheduled appointments");

            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("Cancellation reason is required");

            Status = AppointmentStatus.Cancelled;
            CancellationReason = reason;
            UpdateTimestamp(currentUtcTime);
        }

        public void Reschedule(DateTime newDate, TimeSlot newTimeSlot, DateTime currentUtcTime)
        {
            if (Status != AppointmentStatus.Scheduled)
                throw new BusinessRuleException("Can only reschedule scheduled appointments");

            ValidateBusinessRules(newDate, newTimeSlot, currentUtcTime);

            AppointmentDate = newDate.Date;
            StartTime = newTimeSlot.StartTime;
            EndTime = newTimeSlot.EndTime;
            UpdateTimestamp(currentUtcTime);
        }

        public void MarkAsCompleted(DateTime currentUtcTime)
        {
            if (Status != AppointmentStatus.Scheduled)
                throw new BusinessRuleException("Can only complete scheduled appointments");

            if (AppointmentDateTime > currentUtcTime)
                throw new BusinessRuleException("Cannot complete future appointments");

            Status = AppointmentStatus.Completed;
            UpdateTimestamp(currentUtcTime);
        }

        public void MarkAsNoShow(DateTime currentUtcTime)
        {
            if (Status != AppointmentStatus.Scheduled)
                throw new BusinessRuleException("Can only mark scheduled appointments as no-show");

            if (AppointmentDateTime.AddMinutes(15) > currentUtcTime)
                throw new BusinessRuleException("Cannot mark as no-show until 15 minutes after appointment time");

            Status = AppointmentStatus.NoShow;
            UpdateTimestamp(currentUtcTime);
        }

        public bool ConflictsWith(TimeSlot otherTimeSlot) => TimeSlot.Overlaps(otherTimeSlot);
    }
}