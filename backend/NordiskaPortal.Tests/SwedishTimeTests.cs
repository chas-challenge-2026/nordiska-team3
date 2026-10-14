using System.Globalization;
using FluentAssertions;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class SwedishTimeTests
{
    [Theory]
    [InlineData("2026-01-15T12:00:00", "2026-01-15")]
    [InlineData("2025-12-31T22:59:00", "2025-12-31")]
    [InlineData("2025-12-31T23:00:00", "2026-01-01")]
    [InlineData("2026-06-30T21:59:00", "2026-06-30")]
    [InlineData("2026-06-30T22:00:00", "2026-07-01")]
    public void DateOf_ReturnsTheSwedishCalendarDayOfAUtcMoment(string utc, string expected)
    {
        var moment = DateTime.SpecifyKind(DateTime.Parse(utc, CultureInfo.InvariantCulture), DateTimeKind.Utc);

        SwedishTime.DateOf(moment).Should().Be(DateOnly.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void DateOf_TreatsAMomentWithoutKindAsUtc()
    {
        var moment = new DateTime(2025, 12, 31, 23, 30, 0, DateTimeKind.Unspecified);

        SwedishTime.DateOf(moment).Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void StartOfDayUtc_IsMidnightInStockholmExpressedInUtc()
    {
        SwedishTime.StartOfDayUtc(new DateOnly(2026, 1, 1)).Should().Be(new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc));
        SwedishTime.StartOfDayUtc(new DateOnly(2026, 7, 1)).Should().Be(new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TodayIn_UsesTheSwedishDateOfTheClock()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero));

        SwedishTime.TodayIn(clock).Should().Be(new DateOnly(2027, 1, 1));
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