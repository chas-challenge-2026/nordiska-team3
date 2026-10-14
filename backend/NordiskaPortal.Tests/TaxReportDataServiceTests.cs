using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class TaxReportDataServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    public TaxReportDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _context.Users.Add(new User
        {
            Id = _userId,
            PersonalNumber = "enc",
            FirstName = "Test",
            LastName = "Kund",
            Email = "test@example.com",
            PinHash = "hash"
        });
        _context.Accounts.Add(new Account
        {
            Id = _accountId,
            UserId = _userId,
            AccountNumber = "NKM-00001",
            AccountType = "SAVINGS",
            Name = "Sparkonto"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetTaxReportDataAsync_FillsTheHeaderBankAndCustomer()
    {
        var reportId = Guid.NewGuid();

        var report = await CreateService().GetTaxReportDataAsync(_userId, 2025, reportId);

        report.schemaVersion.Should().Be(1);
        report.reportId.Should().Be(reportId);
        report.reportYear.Should().Be(2025);
        report.Currency.Should().Be("SEK");
        report.Bank.Name.Should().Be("Nordiska Sparbanken");
        report.Bank.OrganizationNumber.Should().Be("556000-0000");
        report.Customer.Id.Should().Be(_userId);
        report.Customer.DisplayName.Should().Be("Test Kund");
        report.Summary.CapitalTaxRate.Should().Be("0.30");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_WhenTheUserDoesNotExist_Throws()
    {
        var act = () => CreateService().GetTaxReportDataAsync(Guid.NewGuid(), 2025, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("User not found.");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_SumsDepositsWithdrawalsAndInterestOfTheYear()
    {
        await AddEntryAsync(new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc), 5000m, "DEPOSIT");
        await AddEntryAsync(new DateTime(2025, 4, 1, 8, 0, 0, DateTimeKind.Utc), 1000m, "DEPOSIT");
        await AddEntryAsync(new DateTime(2025, 5, 1, 8, 0, 0, DateTimeKind.Utc), -750m, "WITHDRAWAL");
        await AddEntryAsync(new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc), 100m, "INTEREST");

        var summary = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Summary;

        summary.TotalDeposits.Should().Be("6000.00");
        summary.TotalWithdrawals.Should().Be("750.00");
        summary.InterestIncome.Should().Be("100.00");
        summary.CapitalTax.Should().Be("30.00");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_RoundsTheCapitalTaxAwayFromZero()
    {
        await AddEntryAsync(new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc), 123.45m, "INTEREST");

        var summary = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Summary;

        summary.CapitalTax.Should().Be("37.04");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_WithoutInterestEntries_ShowsZeroInterestAndTax()
    {
        await AddEntryAsync(new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc), 5000m, "DEPOSIT");

        var summary = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Summary;

        summary.InterestIncome.Should().Be("0.00");
        summary.CapitalTax.Should().Be("0.00");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_ListsOpeningAndClosingBalanceAndTheTransactionsOfTheYear()
    {
        await AddEntryAsync(new DateTime(2024, 11, 1, 8, 0, 0, DateTimeKind.Utc), 2000m, "DEPOSIT");
        await AddEntryAsync(new DateTime(2025, 2, 1, 8, 0, 0, DateTimeKind.Utc), 500m, "DEPOSIT");
        await AddEntryAsync(new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc), -300m, "WITHDRAWAL");
        await AddEntryAsync(new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc), 9999m, "DEPOSIT");

        var account = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Accounts.Single();

        account.Id.Should().Be(_accountId);
        account.DisplayNumber.Should().Be("NKM-00001");
        account.AccountType.Should().Be("SAVINGS");
        account.OpeningBalance.Should().Be("2000.00");
        account.ClosingBalance.Should().Be("2200.00");
        account.Transactions.Select(t => t.Amount).Should().Equal("500.00", "-300.00");
        account.Transactions.Select(t => t.Type).Should().Equal("DEPOSIT", "WITHDRAWAL");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_TheTaxYearFollowsStockholmTime()
    {
        await AddEntryAsync(new DateTime(2025, 12, 31, 23, 30, 0, DateTimeKind.Utc), 1000m, "DEPOSIT");

        var report2025 = await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid());
        var report2026 = await CreateService().GetTaxReportDataAsync(_userId, 2026, Guid.NewGuid());

        report2025.Summary.TotalDeposits.Should().Be("0.00");
        report2025.Accounts.Single().ClosingBalance.Should().Be("0.00");
        report2026.Summary.TotalDeposits.Should().Be("1000.00");
        report2026.Accounts.Single().OpeningBalance.Should().Be("0.00");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_OnlyCountsCompletedTransactions()
    {
        await AddEntryAsync(new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc), 500m, "DEPOSIT");
        await AddEntryAsync(new DateTime(2025, 3, 2, 8, 0, 0, DateTimeKind.Utc), 700m, "DEPOSIT", "PENDING");
        await AddEntryAsync(new DateTime(2025, 3, 3, 8, 0, 0, DateTimeKind.Utc), -200m, "WITHDRAWAL");

        var summary = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Summary;

        summary.TotalDeposits.Should().Be("500.00");
        summary.TotalWithdrawals.Should().Be("200.00");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_LeavesOutOtherCustomersAccounts()
    {
        var otherAccountId = Guid.NewGuid();
        _context.Accounts.Add(new Account
        {
            Id = otherAccountId,
            UserId = Guid.NewGuid(),
            AccountNumber = "NKM-00002",
            AccountType = "SAVINGS",
            Name = "Annans konto"
        });
        await _context.SaveChangesAsync();
        await AddEntryAsync(new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc), 8000m, "DEPOSIT", accountId: otherAccountId);

        var report = await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid());

        report.Accounts.Select(a => a.DisplayNumber).Should().Equal("NKM-00001");
        report.Summary.TotalDeposits.Should().Be("0.00");
    }

    private TaxReportDataService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bank:Name"] = "Nordiska Sparbanken",
                ["Bank:OrganizationNumber"] = "556000-0000",
                ["Bank:Address"] = "Storgatan 1",
                ["Bank:Phone"] = "08-000 00 00",
                ["Bank:Email"] = "bank@example.com"
            })
            .Build();

        return new TaxReportDataService(_context, configuration);
    }

    private async Task AddEntryAsync(DateTime createdAt, decimal amount, string entryType, string status = "COMPLETED", Guid? accountId = null)
    {
        var targetAccount = accountId ?? _accountId;
        var transaction = new Transaction
        {
            AccountId = targetAccount,
            Amount = amount,
            TransactionType = entryType,
            Status = status,
            CreatedAt = createdAt
        };
        _context.Transactions.Add(transaction);
        _context.LedgerEntries.Add(new LedgerEntry
        {
            AccountId = targetAccount,
            TransactionId = transaction.Id,
            Amount = amount,
            EntryType = entryType,
            Description = entryType,
            CreatedAt = createdAt
        });
        await _context.SaveChangesAsync();
    }
}