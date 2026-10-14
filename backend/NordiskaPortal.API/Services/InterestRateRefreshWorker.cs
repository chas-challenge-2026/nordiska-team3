namespace NordiskaPortal.API.Services;

// Runs the refresh when the app has started and then every Riksbank:RefreshHours hours (12 by default).
public class InterestRateRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public InterestRateRefreshWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Lets the app finish starting before the first call to Riksbanken.
        await Task.Yield();

        var hours = Math.Max(1, _configuration.GetValue("Riksbank:RefreshHours", 12));
        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));

        try
        {
            do
            {
                using var scope = _scopeFactory.CreateScope();
                var refresher = scope.ServiceProvider.GetRequiredService<InterestRateRefresher>();
                await refresher.RefreshAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // The app is shutting down.
        }
    }
}