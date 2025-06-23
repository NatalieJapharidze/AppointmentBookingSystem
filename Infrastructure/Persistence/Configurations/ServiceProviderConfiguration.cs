using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ServiceProviderConfiguration : IEntityTypeConfiguration<ServiceProvider>
    {
        public void Configure(EntityTypeBuilder<ServiceProvider> builder)
        {
            builder.ToTable("service_providers");

            builder.HasKey(sp => sp.Id);

            builder.Property(sp => sp.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(sp => sp.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(sp => sp.Email)
                .HasColumnName("email")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(sp => sp.Specialty)
                .HasColumnName("specialty")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sp => sp.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(sp => sp.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(sp => sp.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(sp => sp.Email)
                .IsUnique()
                .HasDatabaseName("IX_service_providers_email_unique");

            builder.HasIndex(sp => sp.IsActive)
                .HasDatabaseName("IX_service_providers_is_active");

            builder.HasIndex(sp => sp.Specialty)
                .HasDatabaseName("IX_service_providers_specialty");

            builder.HasMany<WorkingHours>()
                .WithOne()
                .HasForeignKey(wh => wh.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany<BlockedTime>()
                .WithOne()
                .HasForeignKey(bt => bt.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany<Appointment>()
                .WithOne()
                .HasForeignKey(a => a.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}