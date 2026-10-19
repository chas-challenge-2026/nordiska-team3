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

public class DemoDataSeederTests
{
    private const string DemoEmailEnd = "@demo.example.com";
    private static readonly DateTimeOffset Today = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
    private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    private readonly ApplicationDbContext _context;
    private readonly PersonalNumberProtector _protector;

    public DemoDataSeederTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _protector = new PersonalNumberProtector(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["PersonalNumberProtection:Key"] = Key })
                .Build(),
            NullLogger<PersonalNumberProtector>.Instance);
    }

    [Fact]
    public async Task SeedAsync_OnAnEmptyDatabase_AddsFourCustomersWithTenAccounts()
    {
        var added = await CreateSeeder().SeedAsync();

        added.Should().BeTrue();
        (await DemoUsers().CountAsync()).Should().Be(4);
        (await _context.Accounts.CountAsync()).Should().Be(10);

        var anna = await _context.Users.SingleAsync(u => u.Email == "anna.lindqvist" + DemoEmailEnd);
        var annasAccounts = await _context.Accounts.Where(a => a.UserId == anna.Id).ToListAsync();
        annasAccounts.Select(a => a.AccountType).Should().BeEquivalentTo(new[] { "CHECKING", "SAVINGS", "SAVINGS" });
    }

    [Fact]
    public async Task SeedAsync_StoresPersonalNumbersEncryptedAndFindableByHash()
    {
        await CreateSeeder().SeedAsync();

        var anna = await _context.Users.SingleAsync(u => u.PersonalNumberHash == _protector.ComputeHash("19880312-4421"));

        anna.PersonalNumber.Should().StartWith(PersonalNumberProtector.Prefix);
        anna.PersonalNumber.Should().NotContain("19880312");
        _protector.Unprotect(anna.PersonalNumber).Should().Be("19880312-4421");
        BCrypt.Net.BCrypt.Verify("1234", anna.PinHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_RunTwice_AddsNothingTheSecondTime()
    {
        await CreateSeeder().SeedAsync();
        var transactions = await _context.Transactions.CountAsync();
        var ledgerEntries = await _context.LedgerEntries.CountAsync();

        var addedAgain = await CreateSeeder().SeedAsync();

        addedAgain.Should().BeFalse();
        (await DemoUsers().CountAsync()).Should().Be(4);
        (await _context.Accounts.CountAsync()).Should().Be(10);
        (await _context.Transactions.CountAsync()).Should().Be(transactions);
        (await _context.LedgerEntries.CountAsync()).Should().Be(ledgerEntries);
    }

    [Fact]
    public async Task SeedAsync_WhenAnAccountNumberIsAlreadyInUse_AddsNothing()
    {
        var other = new User
        {
            PersonalNumber = "x",
            PersonalNumberHash = "other-hash",
            FirstName = "Annan",
            LastName = "Kund",
            Email = "annan@example.com",
            PinHash = "hash"
        };
        _context.Users.Add(other);
        _context.Accounts.Add(new Account { UserId = other.Id, AccountNumber = "NKM-20001", AccountType = "CHECKING", Name = "Konto" });
        await _context.SaveChangesAsync();

        var added = await CreateSeeder().SeedAsync();

        added.Should().BeFalse();
        (await DemoUsers().CountAsync()).Should().Be(0);
        (await _context.Accounts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_GivesEveryAccountAHistoryThatNeverGoesBelowZero()
    {
        await CreateSeeder().SeedAsync();

        var entries = await _context.LedgerEntries.ToListAsync();

        foreach (var account in entries.GroupBy(e => e.AccountId))
        {
            var balance = 0m;
            foreach (var entry in account.OrderBy(e => e.CreatedAt))
            {
                balance += entry.Amount;
                balance.Should().BeGreaterThanOrEqualTo(0m);
            }
        }

        entries.Select(e => e.AccountId).Distinct().Should().HaveCount(10);
    }

    [Fact]
    public async Task SeedAsync_GivesAnnaTheBalancesTheDemoDescribes()
    {
        await CreateSeeder().SeedAsync();

        (await BalanceOf("NKM-20001")).Should().Be(71200m);
        (await BalanceOf("NKM-20002")).Should().Be(251794.36m);
        (await BalanceOf("NKM-20003")).Should().Be(6000m);
    }

    [Fact]
    public async Task SeedAsync_BooksEveryTransactionWithOneMatchingLedgerEntry()
    {
        await CreateSeeder().SeedAsync();

        var transactions = await _context.Transactions.ToListAsync();
        var entries = await _context.LedgerEntries.ToListAsync();

        entries.Should().HaveCount(transactions.Count);

        foreach (var transaction in transactions)
        {
            var entry = entries.Single(e => e.TransactionId == transaction.Id);
            entry.AccountId.Should().Be(transaction.AccountId);
            entry.EntryType.Should().Be(transaction.TransactionType);
            Math.Abs(entry.Amount).Should().Be(transaction.Amount);
            transaction.Status.Should().Be("COMPLETED");
        }
    }

    [Fact]
    public async Task SeedAsync_BooksEveryTransferAsOneOutAndOneInWithTheSameAmount()
    {
        await CreateSeeder().SeedAsync();

        var transfers = await _context.Transactions.Where(t => t.TransferId != null).ToListAsync();

        transfers.Should().NotBeEmpty();

        foreach (var pair in transfers.GroupBy(t => t.TransferId))
        {
            pair.Should().HaveCount(2);
            pair.Select(t => t.TransactionType).Should().BeEquivalentTo(new[] { "TRANSFER_OUT", "TRANSFER_IN" });
            pair.Select(t => t.Amount).Distinct().Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task SeedAsync_LabelsTransfersLikeTheAppDoes()
    {
        await CreateSeeder().SeedAsync();

        var annaChecking = await _context.Accounts.SingleAsync(a => a.AccountNumber == "NKM-20001");
        var annaSavings = await _context.Accounts.SingleAsync(a => a.AccountNumber == "NKM-20002");

        var toErik = await _context.Transactions.SingleAsync(t =>
            t.AccountId == annaChecking.Id && t.TransactionType == "TRANSFER_OUT" && t.Amount == 1200m);
        var fromErik = await _context.Transactions.SingleAsync(t =>
            t.AccountId == annaChecking.Id && t.TransactionType == "TRANSFER_IN" && t.Amount == 450m);
        var fromSavings = await _context.Transactions.FirstAsync(t =>
            t.AccountId == annaChecking.Id && t.TransactionType == "TRANSFER_IN" && t.Amount == 4000m);
        var toSemester = await _context.Transactions.SingleAsync(t =>
            t.AccountId == annaSavings.Id && t.TransactionType == "TRANSFER_OUT" && t.Amount == 10000m);

        toErik.CounterpartyLabel.Should().Be("Erik J.");
        fromErik.CounterpartyLabel.Should().Be("Erik J.");
        fromSavings.CounterpartyLabel.Should().Be("Semesterkassan");
        toSemester.CounterpartyLabel.Should().Be("Semesterkassan");
    }

    [Fact]
    public async Task SeedAsync_CoversBothTaxYearsSoBothReportsHaveContent()
    {
        await CreateSeeder().SeedAsync();

        var entries = await _context.LedgerEntries.ToListAsync();

        entries.Should().Contain(e => e.CreatedAt.Year == 2025 && e.EntryType == "DEPOSIT");
        entries.Should().Contain(e => e.CreatedAt.Year == 2025 && e.EntryType == "WITHDRAWAL");
        entries.Should().Contain(e => e.CreatedAt.Year == 2025 && e.EntryType == "INTEREST");
        entries.Should().Contain(e => e.CreatedAt.Year == 2026 && e.EntryType == "WITHDRAWAL");
        entries.Should().Contain(e => e.CreatedAt.Year == 2026 && e.EntryType == "TRANSFER_OUT");
        entries.Should().NotContain(e => e.CreatedAt > Today.UtcDateTime);
    }

    [Fact]
    public async Task SeedAsync_SkipsEventsThatHaveNotHappenedYet()
    {
        var earlyJanuary = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

        await CreateSeeder(earlyJanuary).SeedAsync();

        var entries = await _context.LedgerEntries.ToListAsync();

        entries.Should().NotBeEmpty();
        entries.Should().NotContain(e => e.CreatedAt >= new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task SeedAsync_GivesTheSavingsAccountsInterestToShowAndTheCheckingAccountsNone()
    {
        await CreateSeeder().SeedAsync();

        _context.InterestRateObservations.AddRange(
            new InterestRateObservation { SeriesId = "SECBREPOEFF", Date = new DateOnly(2025, 12, 31), Value = 2.00m, FetchedAt = Today.UtcDateTime });
        _context.AccountInterestPolicies.AddRange(
            new AccountInterestPolicy { AccountType = "SAVINGS", EffectiveFrom = new DateOnly(2026, 1, 1), SpreadPercentagePoints = -0.5m },
            new AccountInterestPolicy { AccountType = "CHECKING", EffectiveFrom = new DateOnly(2026, 1, 1), SpreadPercentagePoints = -100m });
        await _context.SaveChangesAsync();

        var anna = await _context.Users.SingleAsync(u => u.Email == "anna.lindqvist" + DemoEmailEnd);
        var accounts = (await _context.Accounts.Where(a => a.UserId == anna.Id).ToListAsync())
            .Select(a => new AccountDto(a.Id, a.AccountNumber, a.AccountType, a.Name, a.Status, "0.00"))
            .ToList();

        var service = new InterestService(
            _context, new ConfigurationBuilder().Build(), new TestClock(Today), NullLogger<InterestService>.Instance);

        var result = await service.AddInterestAsync(new AccountsResponseDto(accounts));

        foreach (var account in result.Accounts.Where(a => a.AccountType == "SAVINGS"))
        {
            decimal.Parse(account.AccruedInterest!, System.Globalization.CultureInfo.InvariantCulture).Should().BeGreaterThan(0m);
        }

        result.Accounts.Single(a => a.AccountType == "CHECKING").AccruedInterest.Should().Be("0.00");
    }

    [Fact]
    public async Task SeedAsync_BooksTheInterestFor2025OnTheSavingsAccountsThatHadMoney()
    {
        await CreateSeeder().SeedAsync();

        var annaSavings = await _context.Accounts.SingleAsync(a => a.AccountNumber == "NKM-20002");
        var interest = await _context.LedgerEntries.SingleAsync(l => l.AccountId == annaSavings.Id && l.EntryType == "INTEREST");

        interest.Amount.Should().Be(1794.36m);
        interest.Description.Should().Be("R\u00e4nta 2025");
        interest.CreatedAt.Should().Be(new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc));

        var payout = await _context.InterestPayouts.SingleAsync(p => p.AccountId == annaSavings.Id);
        payout.Year.Should().Be(2025);
        payout.Amount.Should().Be(1794.36m);
        payout.TransactionId.Should().Be(interest.TransactionId);

        var withInterest = await _context.LedgerEntries.Where(l => l.EntryType == "INTEREST").Select(l => l.AccountId).Distinct().ToListAsync();
        var numbers = await _context.Accounts.Where(a => withInterest.Contains(a.Id)).Select(a => a.AccountNumber).ToListAsync();
        numbers.Should().BeEquivalentTo(new[] { "NKM-20002", "NKM-30002", "NKM-40002", "NKM-50002" });
    }

    [Fact]
    public async Task SeedAsync_GivesAnnaATaxReportFor2025WithTheWholeYearAndTheInterest()
    {
        await CreateSeeder().SeedAsync();
        var anna = await _context.Users.SingleAsync(u => u.Email == "anna.lindqvist" + DemoEmailEnd);

        var report = await CreateTaxService().GetTaxReportDataAsync(anna.Id, 2025, Guid.NewGuid());

        report.Summary.TotalDeposits.Should().Be("472000.00");
        report.Summary.TotalWithdrawals.Should().Be("236500.00");
        report.Summary.InterestIncome.Should().Be("1794.36");
        report.Summary.CapitalTax.Should().Be("538.31");
        report.Accounts.Should().HaveCount(3);
    }

    [Fact]
    public async Task SeedAsync_GivesAnnaATaxReportFor2026WithoutInterestBecauseTheYearIsNotPaidYet()
    {
        await CreateSeeder().SeedAsync();
        var anna = await _context.Users.SingleAsync(u => u.Email == "anna.lindqvist" + DemoEmailEnd);

        var report = await CreateTaxService().GetTaxReportDataAsync(anna.Id, 2026, Guid.NewGuid());

        report.Summary.InterestIncome.Should().Be("0.00");
        report.Summary.TotalDeposits.Should().NotBe("0.00");
    }

    private DemoDataSeeder CreateSeeder(DateTimeOffset? now = null) =>
        new(_context, _protector, new TestClock(now ?? Today), NullLogger<DemoDataSeeder>.Instance);

    private TaxReportDataService CreateTaxService() =>
        new(
            _context,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Bank:Name"] = "Nordiska Sparbanken",
                    ["Bank:OrganizationNumber"] = "556000-0000",
                    ["Bank:Address"] = "Storgatan 1",
                    ["Bank:Phone"] = "08-000 00 00",
                    ["Bank:Email"] = "bank@example.com"
                })
                .Build());

    private IQueryable<User> DemoUsers() => _context.Users.Where(u => u.Email.EndsWith(DemoEmailEnd));

    private async Task<decimal> BalanceOf(string accountNumber)
    {
        var account = await _context.Accounts.SingleAsync(a => a.AccountNumber == accountNumber);
        return await _context.LedgerEntries.Where(l => l.AccountId == account.Id).SumAsync(l => l.Amount);
    }
}