using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class TaxReportAccountOrderAndLabelTests
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _userId = Guid.NewGuid();

    public TaxReportAccountOrderAndLabelTests()
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
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetTaxReportDataAsync_ListsTheAccountsInAccountNumberOrder()
    {
        AddAccount("NKM-20001");
        AddAccount("NKM-20003");
        AddAccount("NKM-20002");
        await _context.SaveChangesAsync();

        var report = await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid());

        report.Accounts.Select(a => a.DisplayNumber)
            .Should().Equal("NKM-20001", "NKM-20002", "NKM-20003");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_TransferLinesShowTheNameOfTheOtherPartyAfterTheAccountNumber()
    {
        var accountId = AddAccount("NKM-20001");
        await _context.SaveChangesAsync();
        await AddEntryAsync(accountId, new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            -1200m, "TRANSFER_OUT", "Transfer to NKM-30001", "Erik J.");
        await AddEntryAsync(accountId, new DateTime(2025, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            450m, "TRANSFER_IN", "Transfer from NKM-30001", "Erik J.");

        var account = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Accounts.Single();

        account.Transactions.Select(t => t.Description).Should().Equal(
            "Transfer to NKM-30001 (Erik J.)",
            "Transfer from NKM-30001 (Erik J.)");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_TransferWithoutNameKeepsTheOriginalDescription()
    {
        var accountId = AddAccount("NKM-20001");
        await _context.SaveChangesAsync();
        await AddEntryAsync(accountId, new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            -1200m, "TRANSFER_OUT", "Transfer to NKM-30001", null);

        var account = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Accounts.Single();

        account.Transactions.Single().Description.Should().Be("Transfer to NKM-30001");
    }

    [Fact]
    public async Task GetTaxReportDataAsync_OnlyTransfersGetTheName()
    {
        var accountId = AddAccount("NKM-20001");
        await _context.SaveChangesAsync();
        await AddEntryAsync(accountId, new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            500m, "DEPOSIT", "DEPOSIT", "Erik J.");

        var account = (await CreateService().GetTaxReportDataAsync(_userId, 2025, Guid.NewGuid())).Accounts.Single();

        account.Transactions.Single().Description.Should().Be("DEPOSIT");
    }

    private Guid AddAccount(string accountNumber)
    {
        var id = Guid.NewGuid();
        _context.Accounts.Add(new Account
        {
            Id = id,
            UserId = _userId,
            AccountNumber = accountNumber,
            AccountType = "CHECKING",
            Name = accountNumber
        });
        return id;
    }

    private async Task AddEntryAsync(Guid accountId, DateTime createdAt, decimal amount, string entryType, string description, string? label)
    {
        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = Math.Abs(amount),
            TransactionType = entryType,
            Status = "COMPLETED",
            CreatedAt = createdAt,
            CounterpartyLabel = label
        };
        _context.Transactions.Add(transaction);
        _context.LedgerEntries.Add(new LedgerEntry
        {
            AccountId = accountId,
            TransactionId = transaction.Id,
            Amount = amount,
            EntryType = entryType,
            Description = description,
            CreatedAt = createdAt
        });
        await _context.SaveChangesAsync();
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
}