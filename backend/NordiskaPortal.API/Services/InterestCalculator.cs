namespace NordiskaPortal.API.Services;

public sealed record RatePoint(DateOnly Date, decimal Value);

public sealed record SpreadPoint(DateOnly EffectiveFrom, decimal Spread);

public sealed record BalanceChange(DateOnly Date, decimal Amount);

public static class InterestCalculator
{
    public static decimal RateOn(IReadOnlyList<RatePoint> policyRates, IReadOnlyList<SpreadPoint> spreads, DateOnly day)
    {
        if (policyRates.Count == 0)
        {
            throw new ArgumentException("There must be at least one policy rate.", nameof(policyRates));
        }

        if (spreads.Count == 0)
        {
            throw new ArgumentException("There must be at least one spread.", nameof(spreads));
        }

        var policyRate = policyRates[0].Value;
        foreach (var point in policyRates)
        {
            if (point.Date > day) break;
            policyRate = point.Value;
        }

        var spread = spreads[0].Spread;
        foreach (var point in spreads)
        {
            if (point.EffectiveFrom > day) break;
            spread = point.Spread;
        }

        return Math.Max(0m, policyRate + spread);
    }

    public static decimal AccruedInterest(
        IReadOnlyList<RatePoint> policyRates,
        IReadOnlyList<SpreadPoint> spreads,
        decimal openingBalance,
        IReadOnlyList<BalanceChange> changes,
        DateOnly from,
        DateOnly to)
    {
        var sortedChanges = changes.OrderBy(c => c.Date).ToList();
        var balance = openingBalance;
        var next = 0;
        var total = 0m;

        for (var day = from; day < to; day = day.AddDays(1))
        {
            while (next < sortedChanges.Count && sortedChanges[next].Date <= day)
            {
                balance += sortedChanges[next].Amount;
                next++;
            }

            if (balance <= 0m) continue;

            var rate = RateOn(policyRates, spreads, day);
            var daysInYear = DateTime.IsLeapYear(day.Year) ? 366 : 365;

            total += balance * rate / 100m / daysInYear;
        }

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}