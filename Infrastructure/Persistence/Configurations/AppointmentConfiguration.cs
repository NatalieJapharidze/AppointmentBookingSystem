using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("appointments");

            builder.HasKey(a => a.Id);

            builder.UsePropertyAccessMode(PropertyAccessMode.PreferProperty);

            builder.Property(a => a.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(a => a.ProviderId)
                .HasColumnName("provider_id")
                .IsRequired();

            builder.Property(a => a.CustomerName)
                .HasColumnName("customer_name")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.CustomerEmail)
                .HasColumnName("customer_email")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.CustomerPhone)
                .HasColumnName("customer_phone")
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.AppointmentDate)
                .HasColumnName("appointment_date")
                .IsRequired();

            builder.Property(a => a.StartTime)
                .HasColumnName("start_time")
                .IsRequired();

            builder.Property(a => a.EndTime)
                .HasColumnName("end_time")
                .IsRequired();

            builder.Property(a => a.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.CancellationReason)
                .HasColumnName("cancellation_reason")
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(a => a.IsRecurring)
                .HasColumnName("is_recurring")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(a => a.ParentAppointmentId)
                .HasColumnName("parent_appointment_id")
                .IsRequired(false);

            builder.Property(a => a.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(a => a.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.Ignore(a => a.TimeSlot);
            builder.Ignore(a => a.DurationMinutes);
            builder.Ignore(a => a.AppointmentDateTime);
            builder.Ignore(a => a.RecurrenceRule);

            builder.HasIndex(a => new { a.ProviderId, a.AppointmentDate, a.StartTime })
                .HasDatabaseName("IX_appointments_provider_date_time");

            builder.HasIndex(a => a.Status)
                .HasDatabaseName("IX_appointments_status");

            builder.HasIndex(a => a.CustomerEmail)
                .HasDatabaseName("IX_appointments_customer_email");

            builder.HasIndex(a => new { a.AppointmentDate, a.Status })
                .HasDatabaseName("IX_appointments_date_status");

            builder.HasIndex(a => a.ParentAppointmentId)
                .HasDatabaseName("IX_appointments_parent_appointment_id");

            builder.HasOne<ServiceProvider>()
                .WithMany()
                .HasForeignKey(a => a.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Appointment>()
                .WithMany()
                .HasForeignKey(a => a.ParentAppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}