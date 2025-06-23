using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task SendReminderEmailAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task SendCancellationEmailAsync(Appointment appointment, string reason, CancellationToken cancellationToken = default);
        Task SendProviderNotificationAsync(Appointment appointment, string action, CancellationToken cancellationToken = default);

    }
}
