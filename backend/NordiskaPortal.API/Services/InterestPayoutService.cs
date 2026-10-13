using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class InterestPayoutService : IInterestPayoutService
{
    public const string InterestEntryType = "INTEREST";
    public const int DefaultFirstPayoutYear = 2026;
    private const int MaxRateAgeDays = 10;

    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InterestPayoutService> _logger;

    public InterestPayoutService(
        ApplicationDbContext context,
        IAuditService auditService,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<InterestPayoutService> logger)
    {
        _context = context;
        _auditService = auditService;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InterestPayoutResult>> PayoutDueYearsAsync(CancellationToken cancellationToken = default)
    {
        var firstYear = _configuration.GetValue("Interest:FirstPayoutYear", DefaultFirstPayoutYear);
        var results = new List<InterestPayoutResult>();

        for (var year = firstYear; year < Today().Year; year++)
        {
            results.Add(await PayoutYearAsync(year, cancellationToken));
        }

        return results;
    }

    public async Task<InterestPayoutResult> PayoutYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var seriesId = _configuration["Riksbank:PolicyRateSeriesId"] ?? InterestRateRefresher.DefaultSeriesId;
        var today = Today();
        var yearStart = new DateOnly(year, 1, 1);

        var periodEnd = new DateOnly(year + 1, 1, 1);

        var storedRates = await _context.InterestRateObservations
            .AsNoTracking()
            .Where(o => o.SeriesId == seriesId && o.Date >= yearStart.AddDays(-31) && o.Date < periodEnd)
            .OrderBy(o => o.Date)
            .Select(o => new { o.Date, o.Value })
            .ToListAsync(cancellationToken);

        if (storedRates.Count == 0)
        {
            return Skipped(year, "No policy rates are stored for the year.");
        }

        var rates = storedRates.Select(o => new RatePoint(o.Date, o.Value)).ToList();
        var latestRate = rates[^1].Date;

        if (today >= periodEnd && latestRate < periodEnd.AddDays(-MaxRateAgeDays))
        {
            return Skipped(year, $"The latest stored policy rate is from {latestRate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}, which is too old.");
        }

        var policies = await _context.AccountInterestPolicies
            .AsNoTracking()
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var spreadsByType = policies
            .GroupBy(p => p.AccountType)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new SpreadPoint(p.EffectiveFrom, p.SpreadPercentagePoints)).ToList());

        var types = spreadsByType.Keys.ToList();

        var alreadyPaidIds = (await _context.InterestPayouts
                .AsNoTracking()
                .Where(p => p.Year == year)
                .Select(p => p.AccountId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Status == "ACTIVE" && types.Contains(a.AccountType))
            .Select(a => new { a.Id, a.UserId, a.AccountType })
            .ToListAsync(cancellationToken);

        int paid = 0, zero = 0, already = 0, failed = 0;
        var total = 0m;

        foreach (var account in accounts)
        {
            if (alreadyPaidIds.Contains(account.Id))
            {
                already++;
                continue;
            }

            try
            {
                var amount = await PayAccountAsync(account.Id, account.UserId, year, rates, spreadsByType[account.AccountType], cancellationToken);

                if (amount is null) already++;
                else if (amount == 0m) zero++;
                else
                {
                    paid++;
                    total += amount.Value;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _context.ChangeTracker.Clear();
                _logger.LogError(ex, "Could not pay interest for account {AccountId} and year {Year}.", account.Id, year);
            }
        }

        if (paid + zero + failed > 0)
        {
            _logger.LogInformation(
                "Interest for {Year}: {Paid} accounts paid (total {Total}), {Zero} without interest, {Failed} failed.",
                year, paid, total.ToString("F2", CultureInfo.InvariantCulture), zero, failed);
        }

        return new InterestPayoutResult(year, paid, zero, already, failed, total, null);
    }

    private async Task<decimal?> PayAccountAsync(
        Guid accountId,
        Guid userId,
        int year,
        List<RatePoint> rates,
        List<SpreadPoint> spreads,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (_context.Database.IsRelational())
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Accounts\" WHERE \"Id\" = {accountId} FOR UPDATE", cancellationToken);
        }

        if (await _context.InterestPayouts.AnyAsync(p => p.AccountId == accountId && p.Year == year, cancellationToken))
        {
            return null;
        }

        var yearStartUtc = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEndUtc = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var openingBalance = await _context.LedgerEntries
            .Where(l => l.AccountId == accountId && l.CreatedAt < yearStartUtc)
            .SumAsync(l => l.Amount, cancellationToken);

        var movements = await _context.LedgerEntries
            .AsNoTracking()
            .Where(l => l.AccountId == accountId && l.CreatedAt >= yearStartUtc && l.CreatedAt < periodEndUtc)
            .Select(l => new { l.CreatedAt, l.Amount })
            .ToListAsync(cancellationToken);

        var changes = movements
            .Select(m => new BalanceChange(DateOnly.FromDateTime(m.CreatedAt), m.Amount))
            .ToList();

        var amount = InterestCalculator.AccruedInterest(
            rates, spreads, openingBalance, changes, new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1));

        var payout = new InterestPayout
        {
            AccountId = accountId,
            Year = year,
            Amount = amount,
            PaidAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        if (amount > 0m)
        {
            var bookedAt = new DateTime(year, 12, 31, 12, 0, 0, DateTimeKind.Utc);

            var transaction = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                TransactionType = InterestEntryType,
                Status = "COMPLETED",
                CreatedAt = bookedAt,
                CompletedAt = bookedAt
            };

            var ledgerEntry = new LedgerEntry
            {
                AccountId = accountId,
                TransactionId = transaction.Id,
                Amount = amount,
                EntryType = InterestEntryType,
                Description = $"R\u00e4nta {year}",
                CreatedAt = bookedAt
            };

            payout.TransactionId = transaction.Id;

            _context.Transactions.Add(transaction);
            _context.LedgerEntries.Add(ledgerEntry);

            await _auditService.AppendAsync(userId, AuditActions.InterestPaid, nameof(Transaction), transaction.Id,
                new { accountId, year, amount = amount.ToString("F2", CultureInfo.InvariantCulture) });
        }

        _context.InterestPayouts.Add(payout);
        await _context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return amount;
    }

    private InterestPayoutResult Skipped(int year, string reason)
    {
        _logger.LogWarning("Interest for {Year} was not paid: {Reason}", year, reason);
        return new InterestPayoutResult(year, 0, 0, 0, 0, 0m, reason);
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
}