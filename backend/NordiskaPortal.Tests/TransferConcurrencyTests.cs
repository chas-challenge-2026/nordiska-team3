using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Postgres;

// Runs real transfers at the same time against a real Postgres database, because the row locks
// in AccountService.TransferAsync cannot be proven with the InMemory provider.
public class TransferConcurrencyTests : IAsyncLifetime
{
    private readonly string _connectionString = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvVar) ?? "";
    private readonly List<Guid> _userIds = new();

    public async Task InitializeAsync()
    {
        if (_connectionString.Length == 0) return;

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    // Removes the test customers again. Their accounts, transactions and ledger entries go with them (cascade).
    public async Task DisposeAsync()
    {
        if (_userIds.Count == 0) return;

        await using var db = CreateContext();
        await db.Users.Where(u => _userIds.Contains(u.Id)).ExecuteDeleteAsync();
    }

    [PostgresFact]
    public async Task Transfers_in_opposite_directions_at_the_same_time_do_not_deadlock_and_keep_the_total()
    {
        var (userA, accountA) = await CreateCustomerWithAccountAsync(1000m);
        var (userB, accountB) = await CreateCustomerWithAccountAsync(1000m);

        var tasks = new List<Task<TransferOperationResult>>();
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => CreateService().TransferAsync(userA.Id, accountA.Id, accountB.AccountNumber, 10m)));
            tasks.Add(Task.Run(() => CreateService().TransferAsync(userB.Id, accountB.Id, accountA.AccountNumber, 10m)));
        }

        var results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.IsSuccess);
        (await GetBalanceAsync(accountA.Id)).Should().Be(1000m);
        (await GetBalanceAsync(accountB.Id)).Should().Be(1000m);
    }

    [PostgresFact]
    public async Task Simultaneous_transfers_cannot_spend_more_than_the_balance()
    {
        var (userA, accountA) = await CreateCustomerWithAccountAsync(100m);
        var (_, accountB) = await CreateCustomerWithAccountAsync(0m);

        // 10 transfers of 30 kr from an account with 100 kr: exactly 3 may go through.
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => CreateService().TransferAsync(userA.Id, accountA.Id, accountB.AccountNumber, 30m)))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.IsSuccess).Should().Be(3);
        results.Where(r => !r.IsSuccess).Should().OnlyContain(r => r.ErrorMessage == "Insufficient funds.");
        (await GetBalanceAsync(accountA.Id)).Should().Be(10m);
        (await GetBalanceAsync(accountB.Id)).Should().Be(90m);

        // No half transfers: every sent amount has a matching received amount.
        await using var db = CreateContext();
        var sent = await db.Transactions.CountAsync(t => t.AccountId == accountA.Id && t.TransactionType == "TRANSFER_OUT");
        var received = await db.Transactions.CountAsync(t => t.AccountId == accountB.Id && t.TransactionType == "TRANSFER_IN");
        sent.Should().Be(3);
        received.Should().Be(3);
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    // Every call gets its own DbContext, as every request does in the real API.
    private AccountService CreateService()
    {
        var db = CreateContext();

        return new AccountService(
            new AccountRepository(db),
            new UserRepository(db),
            new TransactionRepository(db),
            new Repository<LedgerEntry>(db),
            new Repository<Notification>(db),
            db,
            NullLogger<AccountService>.Instance);
    }

    private async Task<(User User, Account Account)> CreateCustomerWithAccountAsync(decimal startBalance)
    {
        await using var db = CreateContext();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PersonalNumber = "test",
            FirstName = "Test",
            LastName = "Kund",
            Email = $"{userId:N}@test.local",
            PinHash = "not-a-real-hash"
        };

        string accountNumber;
        do
        {
            accountNumber = $"NKM-{Random.Shared.Next(10000, 99999)}";
        }
        while (await db.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

        var account = new Account
        {
            UserId = userId,
            AccountNumber = accountNumber,
            AccountType = "CHECKING",
            Name = "Testkonto"
        };

        db.Users.Add(user);
        db.Accounts.Add(account);

        if (startBalance > 0)
        {
            var deposit = new Transaction
            {
                AccountId = account.Id,
                Amount = startBalance,
                TransactionType = "DEPOSIT",
                Status = "COMPLETED",
                CompletedAt = DateTime.UtcNow
            };

            db.Transactions.Add(deposit);
            db.LedgerEntries.Add(new LedgerEntry
            {
                AccountId = account.Id,
                TransactionId = deposit.Id,
                Amount = startBalance,
                EntryType = "DEPOSIT",
                Description = "Test seed"
            });
        }

        await db.SaveChangesAsync();
        _userIds.Add(userId);

        return (user, account);
    }

    private async Task<decimal> GetBalanceAsync(Guid accountId)
    {
        await using var db = CreateContext();

        return await db.LedgerEntries
            .Where(l => l.AccountId == accountId)
            .SumAsync(l => l.Amount);
    }
}