using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ServiceProvider> ServiceProviders => Set<ServiceProvider>();
        public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<BlockedTime> BlockedTimes => Set<BlockedTime>();
        public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ServiceProviderConfiguration());
            modelBuilder.ApplyConfiguration(new WorkingHoursConfiguration());
            modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
            modelBuilder.ApplyConfiguration(new BlockedTimeConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationLogConfiguration());

            modelBuilder.Ignore<RecurrenceRule>();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity.CreatedAt == default)
                        {
                            entry.Entity.GetType()
                                .GetProperty(nameof(BaseEntity.CreatedAt))
                                ?.SetValue(entry.Entity, DateTime.UtcNow);
                        }
                        if (entry.Entity.UpdatedAt == default)
                        {
                            entry.Entity.GetType()
                                .GetProperty(nameof(BaseEntity.UpdatedAt))
                                ?.SetValue(entry.Entity, DateTime.UtcNow);
                        }
                        break;

                    case EntityState.Modified:
                        entry.Entity.GetType()
                            .GetProperty(nameof(BaseEntity.UpdatedAt))
                            ?.SetValue(entry.Entity, DateTime.UtcNow);
                        break;
                }
            }
        }
    }
}