using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class TaxReportBackgroundWorkerTests : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly Mock<ITaxReportProcessingService> _processing = new();
    private readonly TaxReportQueue _queue = new();
    private readonly ServiceProvider _provider;

    public TaxReportBackgroundWorkerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_processing.Object);
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    private TaxReportBackgroundWorker CreateWorker() =>
        new(_queue, _provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<TaxReportBackgroundWorker>.Instance);

    [Fact]
    public async Task ExecuteAsync_PutsUnfinishedReportsBackInTheQueueWhenItStarts()
    {
        var recovered = new TaskCompletionSource();
        _processing.Setup(p => p.RecoverUnfinishedReportsAsync()).Callback(() => recovered.TrySetResult()).ReturnsAsync(0);
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await recovered.Task.WaitAsync(Wait);
        await worker.StopAsync(CancellationToken.None);

        _processing.Verify(p => p.RecoverUnfinishedReportsAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesAReportThatIsQueued()
    {
        var id = Guid.NewGuid();
        var processed = new TaskCompletionSource();
        _processing.Setup(p => p.ProcessReportAsync(id)).Callback(() => processed.TrySetResult());
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        _queue.Enqueue(id);
        await processed.Task.WaitAsync(Wait);
        await worker.StopAsync(CancellationToken.None);

        _processing.Verify(p => p.ProcessReportAsync(id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_KeepsRunningWhenOneReportFails()
    {
        var failing = Guid.NewGuid();
        var next = Guid.NewGuid();
        var processed = new TaskCompletionSource();
        _processing.Setup(p => p.ProcessReportAsync(failing)).ThrowsAsync(new InvalidOperationException("boom"));
        _processing.Setup(p => p.ProcessReportAsync(next)).Callback(() => processed.TrySetResult());
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        _queue.Enqueue(failing);
        _queue.Enqueue(next);
        await processed.Task.WaitAsync(Wait);
        await worker.StopAsync(CancellationToken.None);

        _processing.Verify(p => p.ProcessReportAsync(next), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_KeepsRunningWhenTheRecoveryFails()
    {
        var id = Guid.NewGuid();
        var processed = new TaskCompletionSource();
        _processing.Setup(p => p.RecoverUnfinishedReportsAsync()).ThrowsAsync(new InvalidOperationException("database is down"));
        _processing.Setup(p => p.ProcessReportAsync(id)).Callback(() => processed.TrySetResult());
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        _queue.Enqueue(id);
        await processed.Task.WaitAsync(Wait);
        await worker.StopAsync(CancellationToken.None);

        _processing.Verify(p => p.ProcessReportAsync(id), Times.Once);
    }
}

public class InterestPayoutWorkerTests : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly Mock<IInterestPayoutService> _payouts = new();
    private readonly ServiceProvider _provider;

    public InterestPayoutWorkerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_payouts.Object);
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    private InterestPayoutWorker CreateWorker() =>
        new(_provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<InterestPayoutWorker>.Instance);

    [Fact]
    public async Task ExecuteAsync_PaysOutDueYearsAsSoonAsItStarts()
    {
        var ran = new TaskCompletionSource();
        _payouts
            .Setup(p => p.PayoutDueYearsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => ran.TrySetResult())
            .ReturnsAsync((IReadOnlyList<InterestPayoutResult>)new List<InterestPayoutResult>());
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await ran.Task.WaitAsync(Wait);
        await worker.StopAsync(CancellationToken.None);

        _payouts.Verify(p => p.PayoutDueYearsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenThePayoutFails_StopsCleanlyAnyway()
    {
        var ran = new TaskCompletionSource();
        _payouts
            .Setup(p => p.PayoutDueYearsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                ran.TrySetResult();
                return Task.FromException<IReadOnlyList<InterestPayoutResult>>(new InvalidOperationException("boom"));
            });
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await ran.Task.WaitAsync(Wait);
        var act = () => worker.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

public class InterestRateRefreshWorkerTests : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly Mock<IInterestRateSource> _source = new();
    private readonly ServiceProvider _provider;

    public InterestRateRefreshWorkerTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_source.Object);
        services.AddScoped<InterestRateRefresher>();
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task ExecuteAsync_RefreshesTheRatesAsSoonAsItStarts()
    {
        var ran = new TaskCompletionSource();
        _source
            .Setup(s => s.GetObservationsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Callback(() => ran.TrySetResult())
            .ReturnsAsync((IReadOnlyList<RateObservation>)new List<RateObservation>());
        var worker = new InterestRateRefreshWorker(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _provider.GetRequiredService<IConfiguration>());

        await worker.StartAsync(CancellationToken.None);
        await ran.Task.WaitAsync(Wait);
        await worker.StopAsync(CancellationToken.None);

        _source.Verify(
            s => s.GetObservationsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}