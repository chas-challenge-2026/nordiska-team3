using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services
{
    public class LoggingEmailSender :IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("Simulated email to {ToEmail}: {Subject} - {Body}", toEmail, subject, body);
            return Task.CompletedTask;
        }

    }
}
