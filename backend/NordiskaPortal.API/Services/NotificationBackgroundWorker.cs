using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services
{
    public class NotificationBackgroundWorker : BackgroundService
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan MaxPollBackoff = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RetryDelayUnit = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger <NotificationBackgroundWorker> _logger;

        public NotificationBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consecutiveFailures = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(GetPollDelay(consecutiveFailures), stoppingToken);
                    await ProcessPendingAsync(stoppingToken);
                    consecutiveFailures = 0;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    _logger.LogError(ex, "Notification polling failed ({Failures} in a row), trying again later", consecutiveFailures);
                }
            }
        }

        public async Task ProcessPendingAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var pending = await context.Notifications
                .Include(n => n.User)
                .Where(n => n.Status == "PENDING")
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            foreach (var notification in pending.Where(n => IsDue(n, now)))
            {
                try
                {
                    await emailSender.SendAsync(notification.User.Email, notification.Type, notification.Message);

                    notification.Status = "SENT";
                    notification.SentAt = DateTime.UtcNow;

                    _logger.LogInformation("Notification {NotificationId} sent", notification.Id);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
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

            await context.SaveChangesAsync(cancellationToken);
        }

        private static bool IsDue(Notification notification, DateTime now) =>
            notification.RetryCount == 0
            || now >= notification.CreatedAt + RetryDelayUnit * (Math.Pow(2, notification.RetryCount) - 1);

        private static TimeSpan GetPollDelay(int consecutiveFailures) =>
            consecutiveFailures == 0
                ? PollInterval
                : TimeSpan.FromSeconds(Math.Min(PollInterval.TotalSeconds * Math.Pow(2, consecutiveFailures), MaxPollBackoff.TotalSeconds));
    }
}