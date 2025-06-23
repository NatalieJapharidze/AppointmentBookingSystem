using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs
{
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReminderService starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendReminders(stoppingToken);
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("ReminderService is stopping due to cancellation");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while sending reminders");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("ReminderService stopped");
        }

        private async Task SendReminders(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var dateTimeService = scope.ServiceProvider.GetRequiredService<IDateTimeService>();

            var currentUtcTime = dateTimeService.UtcNow;
            var tomorrow = currentUtcTime.Date.AddDays(1);

            _logger.LogInformation("Checking for reminder emails to send for date: {Date}", tomorrow);

            try
            {
                var appointmentsNeedingReminders = await context.Appointments
                    .Where(a => a.AppointmentDate == tomorrow &&
                               a.Status == AppointmentStatus.Scheduled)
                    .Where(a => !context.NotificationLogs.Any(nl =>
                        nl.AppointmentId == a.Id &&
                        nl.Type == NotificationType.Reminder &&
                        nl.Status == NotificationStatus.Sent))
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Found {Count} appointments needing reminder emails",
                    appointmentsNeedingReminders.Count);

                var successCount = 0;
                var failureCount = 0;

                foreach (var appointment in appointmentsNeedingReminders)
                {
                    try
                    {
                        var notificationLog = NotificationLog.Create(
                            appointment.Id,
                            NotificationType.Reminder,
                            currentUtcTime);

                        context.NotificationLogs.Add(notificationLog);

                        await emailService.SendReminderEmailAsync(appointment, cancellationToken);

                        notificationLog.MarkAsSent(currentUtcTime);

                        successCount++;
                        _logger.LogInformation("Reminder sent successfully for appointment {AppointmentId}",
                            appointment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send reminder for appointment {AppointmentId}",
                            appointment.Id);

                        var failureLog = NotificationLog.Create(
                            appointment.Id,
                            NotificationType.Reminder,
                            currentUtcTime);

                        failureLog.MarkAsFailed(ex.Message, currentUtcTime);
                        context.NotificationLogs.Add(failureLog);

                        failureCount++;
                    }
                }

                await context.SaveChangesAsync(cancellationToken);

                if (successCount > 0 || failureCount > 0)
                {
                    _logger.LogInformation("Reminder processing completed. Success: {SuccessCount}, Failures: {FailureCount}",
                        successCount, failureCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reminder processing");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ReminderService is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}