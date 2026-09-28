using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Services.Interfaces;
using System.Security.AccessControl;

namespace NordiskaPortal.API.Services
{
    public class NotificationBackgroundWorker : BackgroundService
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger <NotificationBackgroundWorker> _logger;

        public NotificationBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(PollInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var pending = await context.Notifications
                    .Include(n => n.User)
                    .Where(n => n.Status == "PENDING")
                    .ToListAsync(stoppingToken);

                foreach ( var notification in pending)
                {
                    try
                    {
                        await emailSender.SendAsync(notification.User.Email, notification.Type, notification.Message);

                        notification.Status = "SENT";
                        notification.SentAt = DateTime.UtcNow;

                        _logger.LogInformation("Notification {NotificationId} sent to {Email}", notification.Id, notification.User.Email);
                    }
                    catch ( Exception ex ) 
                    {
                        notification.RetryCount++;

                        if (notification.RetryCount >= MaxRetries)
                        {
                            notification.Status = "FAILED";
                            _logger.LogError(ex, "Notification {NotificationId} failed permanently after {RetryCount} attempts", notification.Id, notification.RetryCount);
                        }
                        else
                        {
                            _logger.LogWarning(ex, "Notification {NotificationId} failed, attempt {RetryCount}/{MaxRetries} — will retry", notification.Id, notification.RetryCount, MaxRetries);
                        }
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
