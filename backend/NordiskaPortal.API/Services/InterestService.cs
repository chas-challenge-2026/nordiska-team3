using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.Accounts;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class InterestService : IInterestService
{
    public const string VariableRate = "VARIABLE";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InterestService> _logger;

    public InterestService(
        ApplicationDbContext context,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<InterestService> logger)
    {
        _context = context;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AccountsResponseDto> AddInterestAsync(AccountsResponseDto accounts)
    {
        if (accounts.Accounts.Count == 0) return accounts;

        try
        {
            return await BuildAsync(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not calculate interest. The accounts are returned without interest.");
            return accounts;
        }
    }

    private async Task<AccountsResponseDto> BuildAsync(AccountsResponseDto accounts)
    {
        var seriesId = _configuration["Riksbank:PolicyRateSeriesId"] ?? InterestRateRefresher.DefaultSeriesId;
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var yearStart = new DateOnly(today.Year, 1, 1);
        var yearStartUtc = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var earliestRate = yearStart.AddDays(-31);

        var storedRates = await _context.InterestRateObservations
            .AsNoTracking()
            .Where(o => o.SeriesId == seriesId && o.Date >= earliestRate)
            .OrderBy(o => o.Date)
            .Select(o => new { o.Date, o.Value })
            .ToListAsync();

        if (storedRates.Count == 0) return accounts;

        var rates = storedRates.Select(o => new RatePoint(o.Date, o.Value)).ToList();

        var policies = await _context.AccountInterestPolicies
            .AsNoTracking()
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync();

        var spreadsByType = policies
            .GroupBy(p => p.AccountType)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new SpreadPoint(p.EffectiveFrom, p.SpreadPercentagePoints)).ToList());

        var accountIds = accounts.Accounts.Select(a => a.Id).ToList();

        var openingBalances = await _context.LedgerEntries
            .AsNoTracking()
            .Where(l => accountIds.Contains(l.AccountId) && l.CreatedAt < yearStartUtc)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(l => l.Amount) })
            .ToListAsync();

        var movements = await _context.LedgerEntries
            .AsNoTracking()
            .Where(l => accountIds.Contains(l.AccountId) && l.CreatedAt >= yearStartUtc)
            .Select(l => new { l.AccountId, l.CreatedAt, l.Amount })
            .ToListAsync();

        var payoutDate = new DateOnly(today.Year, 12, 31).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var result = new List<AccountDto>();

        foreach (var account in accounts.Accounts)
        {
            if (!spreadsByType.TryGetValue(account.AccountType, out var spreads))
            {
                result.Add(account);
                continue;
            }

            var openingBalance = openingBalances.FirstOrDefault(o => o.AccountId == account.Id)?.Total ?? 0m;

            var changes = movements
                .Where(m => m.AccountId == account.Id)
                .Select(m => new BalanceChange(DateOnly.FromDateTime(m.CreatedAt), m.Amount))
                .ToList();

            var currentRate = InterestCalculator.RateOn(rates, spreads, today);
            var accrued = InterestCalculator.AccruedInterest(rates, spreads, openingBalance, changes, yearStart, today);

            result.Add(account with
            {
                InterestRate = Format(currentRate),
                InterestType = VariableRate,
                NextInterestPayoutDate = payoutDate,
                AccruedInterest = Format(accrued)
            });
        }

        return new AccountsResponseDto(result);
    }

    private static string Format(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
}