using Application.Common.Interfaces;

namespace Infrastructure.Services
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Today => DateTime.Today;
        public TimeOnly TimeNow => TimeOnly.FromDateTime(DateTime.Now);
    }
}
