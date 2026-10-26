using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using HostelComplaintManagementSystem.Data;
using HostelComplaintManagementSystem.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HostelComplaintManagementSystem.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string recipientEmail, string subject, string htmlMessage, string eventType = "General");
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string htmlMessage, string eventType = "General")
        {
            var isConfigured = !string.IsNullOrWhiteSpace(_configuration["EmailSettings:SmtpHost"]);
            bool sent = false;
            string? errorMessage = null;

            if (isConfigured)
            {
                try
                {
                    var host = _configuration["EmailSettings:SmtpHost"]!;
                    var port = int.TryParse(_configuration["EmailSettings:SmtpPort"], out int p) ? p : 587;
                    var user = _configuration["EmailSettings:SmtpUser"];
                    var pass = _configuration["EmailSettings:SmtpPassword"];
                    var from = _configuration["EmailSettings:FromEmail"] ?? "noreply@hostelcare.com";
                    var enableSsl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out bool ssl) && ssl;

                    using var client = new SmtpClient(host, port)
                    {
                        EnableSsl = enableSsl
                    };

                    if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                    {
                        client.Credentials = new NetworkCredential(user, pass);
                    }

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(from, "HostelCare System"),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(recipientEmail);

                    await client.SendMailAsync(mailMessage);
                    sent = true;
                    _logger.LogInformation("[Email Sent] To: {Recipient}, Subject: {Subject}", recipientEmail, subject);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    _logger.LogWarning(ex, "[Email Failed] Could not deliver email to {Recipient}. Error: {Message}", recipientEmail, ex.Message);
                }
            }
            else
            {
                // Development mode fallback: Log simulation clearly without throwing
                _logger.LogInformation("[Email Simulation] (SMTP not configured) Would send to: {Recipient} | Event: {Event} | Subject: {Subject}",
                    recipientEmail, eventType, subject);
                sent = true; // Mark as handled in simulation
            }

            // Always record audit in EmailNotificationLog table via scope
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var log = new EmailNotificationLog
                {
                    RecipientEmail = recipientEmail,
                    Subject = subject,
                    Body = htmlMessage.Length > 1000 ? htmlMessage.Substring(0, 1000) + "..." : htmlMessage,
                    SentAt = DateTime.UtcNow,
                    IsSuccess = sent,
                    ErrorMessage = errorMessage,
                    EventType = eventType
                };

                dbContext.EmailNotificationLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist EmailNotificationLog for {Recipient}", recipientEmail);
            }

            return sent;
        }
    }
}
