using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Postgres;

// Proves that a repeated request with the same Idempotency-Key does not move money twice.
// Needs a real Postgres database: the row lock and the unique index are what make this safe.
[Collection("Postgres")]
public class IdempotencyKeyPostgresTests : IAsyncLifetime
{
    private static readonly AuditKey TestAuditKey = new(new byte[32]);
    private readonly string _connectionString = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvVar) ?? "";
    private readonly List<Guid> _userIds = new();

    public async Task InitializeAsync()
    {
        if (_connectionString.Length == 0) return;

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_userIds.Count == 0) return;

        await using var db = CreateContext();
        await db.Users.Where(u => _userIds.Contains(u.Id)).ExecuteDeleteAsync();
    }

    [PostgresFact]
    public async Task Deposit_repeated_with_the_same_key_is_booked_once()
    {
        var (user, account) = await CreateCustomerWithAccountAsync(0m);

        var first = await CreateService().DepositAsync(user.Id, account.Id, 100m, "key-1");
        var second = await CreateService().DepositAsync(user.Id, account.Id, 100m, "key-1");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.TransactionId.Should().Be(first.TransactionId);
        second.Balance.Should().Be(100m);

        await using var db = CreateContext();
        (await db.Transactions.CountAsync(t => t.AccountId == account.Id && t.IdempotencyKey == "key-1")).Should().Be(1);
        (await db.LedgerEntries.CountAsync(l => l.AccountId == account.Id)).Should().Be(1);
        (await db.Notifications.CountAsync(n => n.UserId == user.Id && n.Type == "DEPOSIT_COMPLETED")).Should().Be(1);
        (await db.AuditEntries.CountAsync(a => a.UserId == user.Id && a.Action == AuditActions.Deposit)).Should().Be(1);
        (await GetBalanceAsync(account.Id)).Should().Be(100m);
    }

    [PostgresFact]
    public async Task Withdrawal_repeated_with_the_same_key_is_answered_again_even_if_the_balance_is_now_too_low()
    {
        var (user, account) = await CreateCustomerWithAccountAsync(100m);

        var first = await CreateService().WithdrawAsync(user.Id, account.Id, 100m, "key-1");
        var second = await CreateService().WithdrawAsync(user.Id, account.Id, 100m, "key-1");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.TransactionId.Should().Be(first.TransactionId);
        second.Balance.Should().Be(0m);

        await using var db = CreateContext();
        (await db.Transactions.CountAsync(t => t.AccountId == account.Id && t.TransactionType == "WITHDRAWAL")).Should().Be(1);
        (await GetBalanceAsync(account.Id)).Should().Be(0m);
    }

    [PostgresFact]
    public async Task Transfer_repeated_with_the_same_key_is_booked_once()
    {
        var (userA, accountA) = await CreateCustomerWithAccountAsync(100m);
        var (_, accountB) = await CreateCustomerWithAccountAsync(0m);

        var first = await CreateService().TransferAsync(userA.Id, accountA.Id, accountB.AccountNumber, 30m, "key-1");
        var second = await CreateService().TransferAsync(userA.Id, accountA.Id, accountB.AccountNumber, 30m, "key-1");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.TransferId.Should().Be(first.TransferId);
        second.FromBalance.Should().Be(70m);

        await using var db = CreateContext();
        (await db.Transactions.CountAsync(t => t.AccountId == accountA.Id && t.TransactionType == "TRANSFER_OUT")).Should().Be(1);
        (await db.Transactions.CountAsync(t => t.AccountId == accountB.Id && t.TransactionType == "TRANSFER_IN")).Should().Be(1);
        (await db.Notifications.CountAsync(n => n.UserId == userA.Id && n.Type == "TRANSFER_SENT")).Should().Be(1);
        (await db.AuditEntries.CountAsync(a => a.UserId == userA.Id && a.Action == AuditActions.Transfer)).Should().Be(1);
        (await GetBalanceAsync(accountA.Id)).Should().Be(70m);
        (await GetBalanceAsync(accountB.Id)).Should().Be(30m);
    }

    [PostgresFact]
    public async Task Same_key_with_a_different_amount_is_rejected_and_books_nothing()
    {
        var (user, account) = await CreateCustomerWithAccountAsync(0m);

        var first = await CreateService().DepositAsync(user.Id, account.Id, 50m, "key-1");
        var second = await CreateService().DepositAsync(user.Id, account.Id, 60m, "key-1");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeFalse();
        second.ErrorMessage.Should().Be("Idempotency key was already used for a different request.");
        (await GetBalanceAsync(account.Id)).Should().Be(50m);
    }

    [PostgresFact]
    public async Task Same_key_for_a_different_kind_of_request_is_rejected()
    {
        var (user, account) = await CreateCustomerWithAccountAsync(0m);

        var deposit = await CreateService().DepositAsync(user.Id, account.Id, 50m, "key-1");
        var withdrawal = await CreateService().WithdrawAsync(user.Id, account.Id, 50m, "key-1");

        deposit.IsSuccess.Should().BeTrue();
        withdrawal.IsSuccess.Should().BeFalse();
        withdrawal.ErrorMessage.Should().Be("Idempotency key was already used for a different request.");
        (await GetBalanceAsync(account.Id)).Should().Be(50m);
    }

    [PostgresFact]
    public async Task Same_key_on_two_different_accounts_is_two_different_requests()
    {
        var (userA, accountA) = await CreateCustomerWithAccountAsync(0m);
        var (userB, accountB) = await CreateCustomerWithAccountAsync(0m);

        var first = await CreateService().DepositAsync(userA.Id, accountA.Id, 10m, "shared-key");
        var second = await CreateService().DepositAsync(userB.Id, accountB.Id, 10m, "shared-key");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.TransactionId.Should().NotBe(first.TransactionId!.Value);
    }

    [PostgresFact]
    public async Task Without_a_key_two_identical_deposits_are_two_deposits()
    {
        var (user, account) = await CreateCustomerWithAccountAsync(0m);

        var first = await CreateService().DepositAsync(user.Id, account.Id, 10m);
        var second = await CreateService().DepositAsync(user.Id, account.Id, 10m);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.TransactionId.Should().NotBe(first.TransactionId!.Value);
        (await GetBalanceAsync(account.Id)).Should().Be(20m);
    }

    [PostgresFact]
    public async Task Ten_simultaneous_deposits_with_the_same_key_are_booked_once()
    {
        var (user, account) = await CreateCustomerWithAccountAsync(0m);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => CreateService().DepositAsync(user.Id, account.Id, 100m, "key-1")))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.IsSuccess);
        results.Select(r => r.TransactionId).Distinct().Should().HaveCount(1);
        (await GetBalanceAsync(account.Id)).Should().Be(100m);

        await using var db = CreateContext();
        (await db.Transactions.CountAsync(t => t.AccountId == account.Id && t.IdempotencyKey == "key-1")).Should().Be(1);
        (await db.AuditEntries.CountAsync(a => a.UserId == user.Id && a.Action == AuditActions.Deposit)).Should().Be(1);
    }

    [PostgresFact]
    public async Task Ten_simultaneous_transfers_with_the_same_key_are_booked_once()
    {
        var (userA, accountA) = await CreateCustomerWithAccountAsync(100m);
        var (_, accountB) = await CreateCustomerWithAccountAsync(0m);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => CreateService().TransferAsync(userA.Id, accountA.Id, accountB.AccountNumber, 30m, "key-1")))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.IsSuccess);
        results.Select(r => r.TransferId).Distinct().Should().HaveCount(1);
        (await GetBalanceAsync(accountA.Id)).Should().Be(70m);
        (await GetBalanceAsync(accountB.Id)).Should().Be(30m);
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

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
            new AuditService(db, TestAuditKey),
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