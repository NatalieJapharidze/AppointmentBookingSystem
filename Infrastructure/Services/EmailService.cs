using System.Net;
using System.Net.Mail;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _emailEnabled;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _emailEnabled = _configuration.GetValue<bool>("EmailSettings:EnableEmailSending", false);
        }

        public async Task SendConfirmationEmailAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            var subject = "Appointment Confirmation";
            var body = GenerateConfirmationEmailBody(appointment);
            await SendEmailAsync(appointment.CustomerEmail, subject, body, "confirmation");
        }

        public async Task SendReminderEmailAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            var subject = "Appointment Reminder - Tomorrow";
            var body = GenerateReminderEmailBody(appointment);
            await SendEmailAsync(appointment.CustomerEmail, subject, body, "reminder");
        }

        public async Task SendCancellationEmailAsync(Appointment appointment, string reason, CancellationToken cancellationToken = default)
        {
            var subject = "Appointment Cancelled";
            var body = GenerateCancellationEmailBody(appointment, reason);
            await SendEmailAsync(appointment.CustomerEmail, subject, body, "cancellation");
        }

        public Task SendProviderNotificationAsync(Appointment appointment, string action, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Provider notification: {Action} for appointment {AppointmentId}", action, appointment.Id);
            return Task.CompletedTask;
        }

        private async Task SendEmailAsync(string email, string subject, string body, string type)
        {
            if (!_emailEnabled)
            {
                _logger.LogInformation("Email sending disabled. Would send {Type} email to {Email}", type, email);
                return;
            }

            try
            {
                using var client = new SmtpClient(_configuration["EmailSettings:SmtpServer"],
                    _configuration.GetValue<int>("EmailSettings:SmtpPort"));

                client.Credentials = new NetworkCredential(
                    _configuration["EmailSettings:Username"],
                    _configuration["EmailSettings:Password"]);
                client.EnableSsl = true;

                var mailMessage = new MailMessage(_configuration["EmailSettings:FromEmail"]!, email, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("{Type} email sent successfully to {Email}", type, email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Type} email to {Email}", type, email);
                throw;
            }
        }

        private string GenerateConfirmationEmailBody(Appointment appointment)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Appointment Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .appointment-details {{ background-color: white; padding: 15px; border-left: 4px solid #4CAF50; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Appointment Confirmed</h1>
        </div>
        <div class='content'>
            <p>Dear {appointment.CustomerName},</p>
            <p>Your appointment has been successfully scheduled. Here are the details:</p>
            
            <div class='appointment-details'>
                <strong>Date:</strong> {appointment.AppointmentDate:dddd, MMMM dd, yyyy}<br>
                <strong>Time:</strong> {appointment.StartTime:hh:mm tt} - {appointment.EndTime:hh:mm tt}
            </div>
            
            <p>Please arrive 10 minutes early for your appointment.</p>
            <p>If you need to reschedule or cancel, please contact us at least 24 hours in advance.</p>
            
            <p>Thank you for choosing our services!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateReminderEmailBody(Appointment appointment)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Appointment Reminder</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .appointment-details {{ background-color: white; padding: 15px; border-left: 4px solid #FF9800; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Appointment Reminder</h1>
        </div>
        <div class='content'>
            <p>Dear {appointment.CustomerName},</p>
            <p>This is a friendly reminder about your upcoming appointment tomorrow:</p>
            
            <div class='appointment-details'>
                <strong>Date:</strong> {appointment.AppointmentDate:dddd, MMMM dd, yyyy}<br>
                <strong>Time:</strong> {appointment.StartTime:hh:mm tt} - {appointment.EndTime:hh:mm tt}
            </div>
            
            <p>Please remember to arrive 10 minutes early.</p>
            <p>If you need to make any changes, please contact us as soon as possible.</p>
            
            <p>We look forward to seeing you!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateCancellationEmailBody(Appointment appointment, string reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Appointment Cancelled</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .appointment-details {{ background-color: white; padding: 15px; border-left: 4px solid #f44336; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Appointment Cancelled</h1>
        </div>
        <div class='content'>
            <p>Dear {appointment.CustomerName},</p>
            <p>We regret to inform you that your appointment has been cancelled:</p>
            
            <div class='appointment-details'>
                <strong>Date:</strong> {appointment.AppointmentDate:dddd, MMMM dd, yyyy}<br>
                <strong>Time:</strong> {appointment.StartTime:hh:mm tt}<br>
                <strong>Reason:</strong> {reason}
            </div>
            
            <p>We apologize for any inconvenience this may cause.</p>
            <p>Please contact us to reschedule your appointment at your convenience.</p>
            
            <p>Thank you for your understanding.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
