using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHours>
    {
        public void Configure(EntityTypeBuilder<WorkingHours> builder)
        {
            builder.ToTable("working_hours");

            builder.HasKey(wh => wh.Id);

            builder.Property(wh => wh.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(wh => wh.ProviderId)
                .HasColumnName("provider_id")
                .IsRequired();

            builder.Property(wh => wh.DayOfWeek)
                .HasColumnName("day_of_week")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(wh => wh.StartTime)
                .HasColumnName("start_time")
                .IsRequired();

            builder.Property(wh => wh.EndTime)
                .HasColumnName("end_time")
                .IsRequired();

            builder.Property(wh => wh.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(wh => wh.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(wh => wh.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);

            builder.HasIndex(wh => new { wh.ProviderId, wh.DayOfWeek, wh.IsActive });
        }
    }
}