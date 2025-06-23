using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Configurations
{
    public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
    {
        public void Configure(EntityTypeBuilder<NotificationLog> builder)
        {
            builder.ToTable("notification_logs");

            builder.HasKey(nl => nl.Id);

            builder.Property(nl => nl.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(nl => nl.AppointmentId)
                .HasColumnName("appointment_id")
                .IsRequired();

            builder.Property(nl => nl.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(nl => nl.SentAt)
                .HasColumnName("sent_at")
                .IsRequired(false);

            builder.Property(nl => nl.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(nl => nl.ErrorMessage)
                .HasColumnName("error_message")
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.Property(nl => nl.RetryCount)
                .HasColumnName("retry_count")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(nl => nl.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(nl => nl.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(nl => nl.AppointmentId)
                .HasDatabaseName("IX_notification_logs_appointment_id");

            builder.HasIndex(nl => new { nl.Type, nl.Status })
                .HasDatabaseName("IX_notification_logs_type_status");

            builder.HasIndex(nl => nl.SentAt)
                .HasDatabaseName("IX_notification_logs_sent_at");

            builder.HasIndex(nl => new { nl.Status, nl.RetryCount })
                .HasDatabaseName("IX_notification_logs_status_retry_count");

            builder.HasOne<Appointment>()
                .WithMany()
                .HasForeignKey(nl => nl.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
