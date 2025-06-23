using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<ServiceProvider> ServiceProviders { get; }
        DbSet<WorkingHours> WorkingHours { get; }
        DbSet<Appointment> Appointments { get; }
        DbSet<BlockedTime> BlockedTimes { get; }
        DbSet<NotificationLog> NotificationLogs { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
