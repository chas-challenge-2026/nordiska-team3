using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class InterestRateRefresherTests
{
    private const string Series = "SECBREPOEFF";
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly ApplicationDbContext _context;
    private readonly Mock<IInterestRateSource> _source = new();

    public InterestRateRefresherTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RefreshAsync_WhenNothingIsStored_FetchesFromDecemberLastYear_AndStoresTheValues()
    {
        _source.Setup(s => s.GetObservationsAsync(Series, new DateOnly(2025, 12, 1), new DateOnly(2026, 10, 8), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RateObservation(new DateOnly(2026, 10, 7), 1.75m),
                new RateObservation(new DateOnly(2026, 10, 8), 1.75m)
            });

        await CreateRefresher().RefreshAsync();

        var rows = await _context.InterestRateObservations.OrderBy(o => o.Date).ToListAsync();
        rows.Should().HaveCount(2);
        rows[0].SeriesId.Should().Be(Series);
        rows[0].Date.Should().Be(new DateOnly(2026, 10, 7));
        rows[1].Value.Should().Be(1.75m);
        rows[1].FetchedAt.Should().Be(Now.UtcDateTime);
    }

    [Fact]
    public async Task RefreshAsync_WhenValuesAreStored_FetchesFromTheLatestDay_AddsNewDays_AndUpdatesRevisedOnes()
    {
        _context.InterestRateObservations.Add(new InterestRateObservation
        {
            SeriesId = Series,
            Date = new DateOnly(2026, 10, 7),
            Value = 1.75m,
            FetchedAt = Now.UtcDateTime.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        _source.Setup(s => s.GetObservationsAsync(Series, new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 8), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RateObservation(new DateOnly(2026, 10, 7), 1.70m),
                new RateObservation(new DateOnly(2026, 10, 8), 1.50m)
            });

        await CreateRefresher().RefreshAsync();

        var rows = await _context.InterestRateObservations.OrderBy(o => o.Date).ToListAsync();
        rows.Should().HaveCount(2);
        rows[0].Value.Should().Be(1.70m);
        rows[0].FetchedAt.Should().Be(Now.UtcDateTime);
        rows[1].Value.Should().Be(1.50m);
    }

    [Fact]
    public async Task RefreshAsync_WhenRiksbankenCannotBeReached_KeepsTheStoredValuesAndDoesNotThrow()
    {
        _context.InterestRateObservations.Add(new InterestRateObservation
        {
            SeriesId = Series,
            Date = new DateOnly(2026, 10, 7),
            Value = 1.75m,
            FetchedAt = Now.UtcDateTime.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        _source.Setup(s => s.GetObservationsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Riksbanken is down"));

        var act = () => CreateRefresher().RefreshAsync();

        await act.Should().NotThrowAsync();
        var rows = await _context.InterestRateObservations.ToListAsync();
        rows.Should().ContainSingle().Which.Value.Should().Be(1.75m);
    }

    [Fact]
    public async Task RefreshAsync_WhenTheAnswerIsEmpty_StoresNothing()
    {
        _source.Setup(s => s.GetObservationsAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RateObservation>());

        await CreateRefresher().RefreshAsync();

        (await _context.InterestRateObservations.CountAsync()).Should().Be(0);
    }

    private InterestRateRefresher CreateRefresher()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Riksbank:PolicyRateSeriesId"] = Series })
            .Build();

        return new InterestRateRefresher(
            _context,
            _source.Object,
            configuration,
            new FixedTimeProvider(Now),
            NullLogger<InterestRateRefresher>.Instance);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}