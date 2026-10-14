using FluentAssertions;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class InterestCalculatorTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    [Fact]
    public void AccruedInterest_ForHalfAYear_IsBalanceTimesRateTimesDaysOver365()
    {
        // 100 000 x 4 % x 182 / 365 = 1 994.52
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 4.00m) };
        var spreads = new[] { new SpreadPoint(Jan1, 0m) };

        var result = InterestCalculator.AccruedInterest(
            rates, spreads, 100000m, Array.Empty<BalanceChange>(), Jan1, new DateOnly(2026, 7, 2));

        result.Should().Be(1994.52m);
    }

    [Fact]
    public void AccruedInterest_WhenThePolicyRateChanges_UsesTheRateThatAppliedEachDay()
    {
        // 181 days at 2 %, then 184 days at 1 %: 99.178 + 50.411 = 149.59
        var rates = new[]
        {
            new RatePoint(new DateOnly(2025, 12, 31), 2.00m),
            new RatePoint(new DateOnly(2026, 7, 1), 1.00m)
        };
        var spreads = new[] { new SpreadPoint(Jan1, 0m) };

        var result = InterestCalculator.AccruedInterest(
            rates, spreads, 10000m, Array.Empty<BalanceChange>(), Jan1, new DateOnly(2027, 1, 1));

        result.Should().Be(149.59m);
    }

    [Fact]
    public void AccruedInterest_CountsADepositFromTheDayItIsMade_EvenWhenTheChangesComeInTheWrongOrder()
    {
        // 1 000 kr at 3.65 % pays 0.10 kr per day. The deposit is made on day 11 and counted until day 20 = 10 days.
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 3.65m) };
        var spreads = new[] { new SpreadPoint(Jan1, 0m) };
        var changes = new[]
        {
            new BalanceChange(new DateOnly(2026, 1, 15), -1000m),
            new BalanceChange(new DateOnly(2026, 1, 11), 1000m)
        };

        var result = InterestCalculator.AccruedInterest(rates, spreads, 0m, changes, Jan1, new DateOnly(2026, 1, 21));

        // 1 000 kr from 11 to 14 January (4 days), then nothing: 0.40
        result.Should().Be(0.40m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void AccruedInterest_WhenTheBalanceIsZeroOrNegative_IsZero(int balance)
    {
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 4.00m) };
        var spreads = new[] { new SpreadPoint(Jan1, 0m) };

        var result = InterestCalculator.AccruedInterest(
            rates, spreads, balance, Array.Empty<BalanceChange>(), Jan1, new DateOnly(2026, 7, 2));

        result.Should().Be(0m);
    }

    [Fact]
    public void AccruedInterest_WhenTheSpreadTakesTheRateBelowZero_IsZero()
    {
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 0.25m) };
        var spreads = new[] { new SpreadPoint(Jan1, -0.5m) };

        var result = InterestCalculator.AccruedInterest(
            rates, spreads, 100000m, Array.Empty<BalanceChange>(), Jan1, new DateOnly(2026, 7, 2));

        result.Should().Be(0m);
    }

    [Fact]
    public void AccruedInterest_InALeapYear_DividesBy366()
    {
        // 36 600 kr at 1 % is 1.00 kr per day, for 366 days
        var rates = new[] { new RatePoint(new DateOnly(2027, 12, 31), 1.00m) };
        var spreads = new[] { new SpreadPoint(new DateOnly(2026, 1, 1), 0m) };

        var result = InterestCalculator.AccruedInterest(
            rates, spreads, 36600m, Array.Empty<BalanceChange>(), new DateOnly(2028, 1, 1), new DateOnly(2029, 1, 1));

        result.Should().Be(366.00m);
    }

    [Fact]
    public void AccruedInterest_WhenFromIsNotBeforeTo_IsZero()
    {
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 4.00m) };
        var spreads = new[] { new SpreadPoint(Jan1, 0m) };

        var result = InterestCalculator.AccruedInterest(
            rates, spreads, 100000m, Array.Empty<BalanceChange>(), Jan1, Jan1);

        result.Should().Be(0m);
    }

    [Fact]
    public void RateOn_IsThePolicyRatePlusTheSpread()
    {
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 1.75m) };
        var spreads = new[] { new SpreadPoint(Jan1, -0.5m) };

        InterestCalculator.RateOn(rates, spreads, new DateOnly(2026, 3, 1)).Should().Be(1.25m);
    }

    [Fact]
    public void RateOn_ADayBeforeTheFirstValueUsesTheFirstValue()
    {
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 1.75m) };
        var spreads = new[] { new SpreadPoint(Jan1, -0.5m) };

        InterestCalculator.RateOn(rates, spreads, new DateOnly(2025, 11, 1)).Should().Be(1.25m);
    }

    [Fact]
    public void RateOn_UsesTheSpreadThatAppliesOnThatDay()
    {
        var rates = new[] { new RatePoint(new DateOnly(2025, 12, 31), 1.75m) };
        var spreads = new[]
        {
            new SpreadPoint(Jan1, -0.5m),
            new SpreadPoint(new DateOnly(2026, 7, 1), -0.25m)
        };

        InterestCalculator.RateOn(rates, spreads, new DateOnly(2026, 6, 30)).Should().Be(1.25m);
        InterestCalculator.RateOn(rates, spreads, new DateOnly(2026, 7, 1)).Should().Be(1.50m);
    }

    [Fact]
    public void RateOn_WhenThereIsNoPolicyRate_Throws()
    {
        var act = () => InterestCalculator.RateOn(
            Array.Empty<RatePoint>(), new[] { new SpreadPoint(Jan1, 0m) }, Jan1);

        act.Should().Throw<ArgumentException>();
    }
}