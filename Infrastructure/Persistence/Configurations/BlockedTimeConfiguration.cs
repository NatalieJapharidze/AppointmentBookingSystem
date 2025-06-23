using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class BlockedTimeConfiguration : IEntityTypeConfiguration<BlockedTime>
    {
        public void Configure(EntityTypeBuilder<BlockedTime> builder)
        {
            builder.ToTable("blocked_times");

            builder.HasKey(bt => bt.Id);

            builder.Property(bt => bt.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(bt => bt.ProviderId)
                .HasColumnName("provider_id")
                .IsRequired();

            builder.Property(bt => bt.StartDateTime)
                .HasColumnName("start_datetime")
                .IsRequired();

            builder.Property(bt => bt.EndDateTime)
                .HasColumnName("end_datetime")
                .IsRequired();

            builder.Property(bt => bt.Reason)
                .HasColumnName("reason")
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(bt => bt.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.HasIndex(bt => new { bt.ProviderId, bt.StartDateTime, bt.EndDateTime });
        }
    }
}