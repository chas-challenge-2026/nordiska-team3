using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class InterestPayoutServiceTests
{
    private const string Series = "SECBREPOEFF";
    private static readonly DateTimeOffset AfterYearEnd = new(2027, 1, 1, 3, 0, 0, TimeSpan.Zero);

    private readonly ApplicationDbContext _context;
    private readonly Mock<IAuditService> _audit = new();
    private readonly Guid _userId = Guid.NewGuid();

    public InterestPayoutServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task PayoutYearAsync_PaysTheWholeYearsInterestOnASavingsAccount()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 36500m);

        var result = await CreateService(AfterYearEnd).PayoutYearAsync(2026);

        // 36 500 kr at 3.5 % is 3.50 kr per day for 365 days
        result.Paid.Should().Be(1);
        result.TotalAmount.Should().Be(1277.50m);
        result.SkippedReason.Should().BeNull();

        var entry = await _context.LedgerEntries.SingleAsync(e => e.EntryType == "INTEREST");
        entry.AccountId.Should().Be(accountId);
        entry.Amount.Should().Be(1277.50m);
        entry.CreatedAt.Should().Be(new DateTime(2026, 12, 31, 12, 0, 0, DateTimeKind.Utc));

        var transaction = await _context.Transactions.SingleAsync();
        transaction.TransactionType.Should().Be("INTEREST");
        transaction.Status.Should().Be("COMPLETED");
        entry.TransactionId.Should().Be(transaction.Id);

        var payout = await _context.InterestPayouts.SingleAsync();
        payout.Year.Should().Be(2026);
        payout.Amount.Should().Be(1277.50m);
        payout.TransactionId.Should().Be(transaction.Id);

        _audit.Verify(a => a.AppendAsync(
            _userId, AuditActions.InterestPaid, nameof(Transaction), transaction.Id, It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task PayoutYearAsync_CountsADepositFromTheDayItWasMade()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc), 36500m);

        var result = await CreateService(AfterYearEnd).PayoutYearAsync(2026);

        // 3.50 kr per day from 1 June to 31 December = 214 days
        result.TotalAmount.Should().Be(749.00m);
    }

    [Fact]
    public async Task PayoutYearAsync_WhenRunTwice_PaysOnlyOnce()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 36500m);
        var service = CreateService(AfterYearEnd);

        await service.PayoutYearAsync(2026);
        var second = await service.PayoutYearAsync(2026);

        second.Paid.Should().Be(0);
        second.AlreadyPaid.Should().Be(1);
        (await _context.LedgerEntries.CountAsync(e => e.EntryType == "INTEREST")).Should().Be(1);
        (await _context.InterestPayouts.CountAsync()).Should().Be(1);
        _audit.Verify(a => a.AppendAsync(
            It.IsAny<Guid?>(), AuditActions.InterestPaid, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task PayoutYearAsync_ForACheckingAccount_RecordsZeroInterestWithoutBookingAnything()
    {
        var accountId = await AddAccountAsync("CHECKING");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 100000m);

        var result = await CreateService(AfterYearEnd).PayoutYearAsync(2026);

        result.Paid.Should().Be(0);
        result.ZeroInterest.Should().Be(1);
        (await _context.LedgerEntries.CountAsync(e => e.EntryType == "INTEREST")).Should().Be(0);
        (await _context.Transactions.CountAsync()).Should().Be(0);

        var payout = await _context.InterestPayouts.SingleAsync();
        payout.Amount.Should().Be(0m);
        payout.TransactionId.Should().BeNull();

        _audit.Verify(a => a.AppendAsync(
            It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task PayoutYearAsync_WithoutStoredRates_IsSkippedAndPaysNothing()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 36500m);

        var result = await CreateService(AfterYearEnd).PayoutYearAsync(2026);

        result.SkippedReason.Should().NotBeNullOrEmpty();
        result.Paid.Should().Be(0);
        (await _context.InterestPayouts.CountAsync()).Should().Be(0);
        (await _context.LedgerEntries.CountAsync(e => e.EntryType == "INTEREST")).Should().Be(0);
    }

    [Fact]
    public async Task PayoutYearAsync_WhenTheLatestRateIsMuchOlderThanYearEnd_IsSkipped()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 11, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 36500m);

        var result = await CreateService(AfterYearEnd).PayoutYearAsync(2026);

        result.SkippedReason.Should().NotBeNullOrEmpty();
        result.Paid.Should().Be(0);
        (await _context.InterestPayouts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task PayoutYearAsync_SkipsAccountsThatAreNotActive()
    {
        var accountId = await AddAccountAsync("SAVINGS", status: "CLOSED");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 36500m);

        var result = await CreateService(AfterYearEnd).PayoutYearAsync(2026);

        result.Paid.Should().Be(0);
        (await _context.InterestPayouts.CountAsync()).Should().Be(0);
        (await _context.LedgerEntries.CountAsync(e => e.EntryType == "INTEREST")).Should().Be(0);
    }

    [Fact]
    public async Task PayoutDueYearsAsync_BeforeTheFirstYearHasEnded_DoesNothing()
    {
        await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 9, 30));

        var results = await CreateService(new DateTimeOffset(2026, 10, 8, 10, 0, 0, TimeSpan.Zero)).PayoutDueYearsAsync();

        results.Should().BeEmpty();
        (await _context.InterestPayouts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task PayoutDueYearsAsync_OnTheFirstOfJanuary_PaysTheYearThatJustEnded()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 36500m);

        var results = await CreateService(AfterYearEnd).PayoutDueYearsAsync();

        results.Should().ContainSingle();
        results[0].Year.Should().Be(2026);
        results[0].Paid.Should().Be(1);
    }

    [Fact]
    public async Task PayoutYearAsync_CountsADepositMadeAfterSwedishMidnightFromTheSwedishDay()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2026, 6, 30, 22, 30, 0, DateTimeKind.Utc), 36500m);

        var result = await CreateService(AfterYearEnd).PayoutYearAsync(2026);

        result.TotalAmount.Should().Be(644.00m);
    }

    [Fact]
    public async Task PayoutDueYearsAsync_PaysTheYearAsSoonAsItIsNewYearInSweden()
    {
        var accountId = await AddAccountAsync("SAVINGS");
        await AddPolicyAsync();
        await AddRatesAsync(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 30));
        await AddLedgerEntryAsync(accountId, new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 36500m);

        var results = await CreateService(new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero)).PayoutDueYearsAsync();

        results.Should().ContainSingle(r => r.Year == 2026 && r.Paid == 1);
    }

    private InterestPayoutService CreateService(DateTimeOffset now) =>
        new(
            _context,
            _audit.Object,
            new ConfigurationBuilder().Build(),
            new FixedTimeProvider(now),
            NullLogger<InterestPayoutService>.Instance);

    private async Task<Guid> AddAccountAsync(string accountType, string status = "ACTIVE")
    {
        var account = new Account
        {
            UserId = _userId,
            AccountNumber = "NKM-" + Guid.NewGuid().ToString("N")[..6],
            AccountType = accountType,
            Name = "Konto",
            Status = status
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account.Id;
    }

    private async Task AddPolicyAsync()
    {
        _context.AccountInterestPolicies.AddRange(
            new AccountInterestPolicy { AccountType = "SAVINGS", EffectiveFrom = new DateOnly(2026, 1, 1), SpreadPercentagePoints = -0.5m },
            new AccountInterestPolicy { AccountType = "CHECKING", EffectiveFrom = new DateOnly(2026, 1, 1), SpreadPercentagePoints = -100m });
        await _context.SaveChangesAsync();
    }

    private async Task AddRatesAsync(params DateOnly[] dates)
    {
        foreach (var date in dates)
        {
            _context.InterestRateObservations.Add(new InterestRateObservation
            {
                SeriesId = Series,
                Date = date,
                Value = 4.00m,
                FetchedAt = AfterYearEnd.UtcDateTime
            });
        }
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