using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services
{
    public class TaxReportBackgroundWorker : BackgroundService
    {
        private readonly TaxReportQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TaxReportBackgroundWorker> _logger;

        public TaxReportBackgroundWorker(TaxReportQueue queue, IServiceScopeFactory scopeFactory, ILogger<TaxReportBackgroundWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var reportId = await _queue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var processingService = scope.ServiceProvider.GetRequiredService<ITaxReportProcessingService>();

                try
                {
                    await processingService.ProcessReportAsync(reportId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process TaxReport {TaxReportId}", reportId);
                }
            }
        }
    }
}
