using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.Accounts;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class InterestServiceTests
{
    private const string Series = "SECBREPOEFF";
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    private readonly ApplicationDbContext _context;

    public InterestServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddInterestAsync_ForASavingsAccount_AddsRateTypePayoutDateAndAccruedInterest()
    {
        var accountId = Guid.NewGuid();
        await AddPolicyRatesAsync();
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 100000m);

        var result = await CreateService().AddInterestAsync(Accounts(Account(accountId, "SAVINGS")));

        var account = result.Accounts.Single();
        account.InterestRate.Should().Be("3.50");
        account.InterestType.Should().Be("VARIABLE");
        account.NextInterestPayoutDate.Should().Be("2026-12-31");
        // 100 000 x 3.5 % x 182 / 365 = 1 745.21
        account.AccruedInterest.Should().Be("1745.21");
    }

    [Fact]
    public async Task AddInterestAsync_CountsDepositsMadeThisYearFromTheDayTheyAreMade()
    {
        var accountId = Guid.NewGuid();
        await AddPolicyRatesAsync();
        await AddLedgerEntryAsync(accountId, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc), 36500m);

        var result = await CreateService().AddInterestAsync(Accounts(Account(accountId, "SAVINGS")));

        // 36 500 kr at 3.5 % is 3.50 kr per day, from 1 June to 1 July = 31 days
        result.Accounts.Single().AccruedInterest.Should().Be("108.50");
    }

    [Fact]
    public async Task AddInterestAsync_ForACheckingAccount_ShowsZeroPercentAndNoInterest()
    {
        var accountId = Guid.NewGuid();
        await AddPolicyRatesAsync();
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 100000m);

        var result = await CreateService().AddInterestAsync(Accounts(Account(accountId, "CHECKING")));

        var account = result.Accounts.Single();
        account.InterestRate.Should().Be("0.00");
        account.AccruedInterest.Should().Be("0.00");
    }

    [Fact]
    public async Task AddInterestAsync_ForAnAccountTypeWithoutAPolicy_LeavesTheAccountUnchanged()
    {
        await AddPolicyRatesAsync();
        var savingsId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var result = await CreateService().AddInterestAsync(
            Accounts(Account(savingsId, "SAVINGS"), Account(otherId, "OTHER")));

        result.Accounts.Single(a => a.Id == savingsId).InterestRate.Should().NotBeNull();
        var other = result.Accounts.Single(a => a.Id == otherId);
        other.InterestRate.Should().BeNull();
        other.InterestType.Should().BeNull();
        other.NextInterestPayoutDate.Should().BeNull();
        other.AccruedInterest.Should().BeNull();
    }

    [Fact]
    public async Task AddInterestAsync_WhenNoPolicyRateIsStored_ReturnsTheAccountsWithoutInterest()
    {
        var result = await CreateService().AddInterestAsync(Accounts(Account(Guid.NewGuid(), "SAVINGS")));

        var account = result.Accounts.Single();
        account.InterestRate.Should().BeNull();
        account.InterestType.Should().BeNull();
        account.NextInterestPayoutDate.Should().BeNull();
        account.AccruedInterest.Should().BeNull();
    }

    [Fact]
    public async Task AddInterestAsync_WithoutAccounts_ReturnsTheSameResponse()
    {
        var empty = Accounts();

        var result = await CreateService().AddInterestAsync(empty);

        result.Should().BeSameAs(empty);
    }

    private InterestService CreateService() =>
        new(
            _context,
            new ConfigurationBuilder().Build(),
            new FixedTimeProvider(Now),
            NullLogger<InterestService>.Instance);

    private async Task AddPolicyRatesAsync()
    {
        _context.InterestRateObservations.AddRange(
            new InterestRateObservation { SeriesId = Series, Date = new DateOnly(2025, 12, 31), Value = 4.00m, FetchedAt = Now.UtcDateTime },
            new InterestRateObservation { SeriesId = Series, Date = new DateOnly(2026, 7, 1), Value = 4.00m, FetchedAt = Now.UtcDateTime });

        _context.AccountInterestPolicies.AddRange(
            new AccountInterestPolicy { AccountType = "SAVINGS", EffectiveFrom = new DateOnly(2026, 1, 1), SpreadPercentagePoints = -0.5m },
            new AccountInterestPolicy { AccountType = "CHECKING", EffectiveFrom = new DateOnly(2026, 1, 1), SpreadPercentagePoints = -100m });

        await _context.SaveChangesAsync();
    }

    private async Task AddLedgerEntryAsync(Guid accountId, DateTime createdAt, decimal amount)
    {
        _context.LedgerEntries.Add(new LedgerEntry
        {
            AccountId = accountId,
            TransactionId = Guid.NewGuid(),
            Amount = amount,
            EntryType = "DEPOSIT",
            Description = "Deposit",
            CreatedAt = createdAt
        });
        await _context.SaveChangesAsync();
    }

    private static AccountDto Account(Guid id, string accountType) =>
        new(id, "NKM-00001", accountType, "Konto", "ACTIVE", "0.00");

    private static AccountsResponseDto Accounts(params AccountDto[] accounts) => new(accounts);

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