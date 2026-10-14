using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Postgres;

[Collection("Postgres")]
public class AccountMoneyPostgresTests : IAsyncLifetime
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
    public async Task Deposit_adds_the_amount_and_writes_transaction_ledger_notification_and_audit_entry()
    {
        var (user, accounts) = await CreateCustomerAsync(0m);
        var account = accounts[0];

        var result = await CreateService().DepositAsync(user.Id, account.Id, 250.50m);

        result.IsSuccess.Should().BeTrue();
        result.Balance.Should().Be(250.50m);
        result.TransactionId.Should().NotBeNull();

        await using var db = CreateContext();
        var transaction = await db.Transactions.SingleAsync(t => t.AccountId == account.Id);
        transaction.Id.Should().Be(result.TransactionId!.Value);
        transaction.TransactionType.Should().Be("DEPOSIT");
        transaction.Status.Should().Be("COMPLETED");
        transaction.Amount.Should().Be(250.50m);
        (await db.LedgerEntries.SingleAsync(l => l.AccountId == account.Id)).Amount.Should().Be(250.50m);
        (await db.Notifications.SingleAsync(n => n.UserId == user.Id)).Type.Should().Be("DEPOSIT_COMPLETED");
        (await db.AuditEntries.CountAsync(a => a.UserId == user.Id && a.Action == AuditActions.Deposit)).Should().Be(1);
        (await AuditChainIsIntactAsync()).Should().BeTrue();
    }

    [PostgresFact]
    public async Task Deposit_to_another_customers_account_is_refused_and_writes_nothing()
    {
        var (_, ownerAccounts) = await CreateCustomerAsync(100m);
        var (other, _) = await CreateCustomerAsync(0m);

        var result = await CreateService().DepositAsync(other.Id, ownerAccounts[0].Id, 50m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account does not exist.");
        (await GetBalanceAsync(ownerAccounts[0].Id)).Should().Be(100m);

        await using var db = CreateContext();
        (await db.AuditEntries.CountAsync(a => a.UserId == other.Id)).Should().Be(0);
    }

    [PostgresFact]
    public async Task Withdrawal_takes_the_amount_and_writes_transaction_ledger_notification_and_audit_entry()
    {
        var (user, accounts) = await CreateCustomerAsync(500m);
        var account = accounts[0];

        var result = await CreateService().WithdrawAsync(user.Id, account.Id, 120.25m);

        result.IsSuccess.Should().BeTrue();
        result.Balance.Should().Be(379.75m);

        await using var db = CreateContext();
        var transaction = await db.Transactions.SingleAsync(t => t.AccountId == account.Id && t.TransactionType == "WITHDRAWAL");
        transaction.Id.Should().Be(result.TransactionId!.Value);
        transaction.Amount.Should().Be(120.25m);
        (await db.LedgerEntries.SingleAsync(l => l.TransactionId == transaction.Id)).Amount.Should().Be(-120.25m);
        (await db.Notifications.SingleAsync(n => n.UserId == user.Id)).Type.Should().Be("WITHDRAWAL_COMPLETED");
        (await db.AuditEntries.CountAsync(a => a.UserId == user.Id && a.Action == AuditActions.Withdrawal)).Should().Be(1);
        (await AuditChainIsIntactAsync()).Should().BeTrue();
    }

    [PostgresFact]
    public async Task Withdrawing_exactly_the_whole_balance_is_allowed()
    {
        var (user, accounts) = await CreateCustomerAsync(100m);

        var result = await CreateService().WithdrawAsync(user.Id, accounts[0].Id, 100m);

        result.IsSuccess.Should().BeTrue();
        result.Balance.Should().Be(0m);
    }

    [PostgresFact]
    public async Task Withdrawal_above_the_balance_is_denied_and_writes_nothing()
    {
        var (user, accounts) = await CreateCustomerAsync(100m);

        var result = await CreateService().WithdrawAsync(user.Id, accounts[0].Id, 100.01m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Insufficient funds.");
        (await GetBalanceAsync(accounts[0].Id)).Should().Be(100m);

        await using var db = CreateContext();
        (await db.Transactions.CountAsync(t => t.AccountId == accounts[0].Id && t.TransactionType == "WITHDRAWAL")).Should().Be(0);
        (await db.AuditEntries.CountAsync(a => a.UserId == user.Id && a.Action == AuditActions.Withdrawal)).Should().Be(0);
    }

    [PostgresFact]
    public async Task Transfer_between_own_accounts_moves_the_money_and_returns_both_balances()
    {
        var (user, accounts) = await CreateCustomerAsync(300m, 50m);
        var from = accounts[0];
        var to = accounts[1];

        var result = await CreateService().TransferAsync(user.Id, from.Id, to.AccountNumber, 100m);

        result.IsSuccess.Should().BeTrue();
        result.FromBalance.Should().Be(200m);
        result.ToBalance.Should().Be(150m);

        await using var db = CreateContext();
        var sent = await db.Transactions.SingleAsync(t => t.AccountId == from.Id && t.TransactionType == "TRANSFER_OUT");
        var received = await db.Transactions.SingleAsync(t => t.AccountId == to.Id && t.TransactionType == "TRANSFER_IN");
        sent.TransferId.Should().Be(result.TransferId);
        received.TransferId.Should().Be(result.TransferId);
        sent.CounterpartyLabel.Should().Be(to.Name);
        received.CounterpartyLabel.Should().Be(from.Name);

        var notifications = await db.Notifications.Where(n => n.UserId == user.Id).ToListAsync();
        notifications.Should().ContainSingle(n => n.Type == "TRANSFER_SENT");
        notifications.Should().NotContain(n => n.Type == "TRANSFER_RECEIVED");
        (await AuditChainIsIntactAsync()).Should().BeTrue();
    }

    [PostgresFact]
    public async Task Transfer_to_another_customer_hides_the_recipient_balance_and_notifies_both()
    {
        var (sender, senderAccounts) = await CreateCustomerAsync(300m);
        var (recipient, recipientAccounts) = await CreateCustomerAsync(0m);

        var result = await CreateService().TransferAsync(sender.Id, senderAccounts[0].Id, recipientAccounts[0].AccountNumber, 100m);

        result.IsSuccess.Should().BeTrue();
        result.FromBalance.Should().Be(200m);
        result.ToBalance.Should().BeNull();
        (await GetBalanceAsync(recipientAccounts[0].Id)).Should().Be(100m);

        await using var db = CreateContext();
        (await db.Notifications.SingleAsync(n => n.UserId == sender.Id)).Type.Should().Be("TRANSFER_SENT");
        (await db.Notifications.SingleAsync(n => n.UserId == recipient.Id)).Type.Should().Be("TRANSFER_RECEIVED");
        (await db.AuditEntries.CountAsync(a => a.UserId == sender.Id && a.Action == AuditActions.Transfer)).Should().Be(1);
        (await AuditChainIsIntactAsync()).Should().BeTrue();
    }

    [PostgresFact]
    public async Task Denied_transfers_write_nothing()
    {
        var (user, accounts) = await CreateCustomerAsync(100m, 0m);
        var (_, otherAccounts) = await CreateCustomerAsync(100m);

        var unknownRecipient = await CreateService().TransferAsync(user.Id, accounts[0].Id, "NKM-00000", 10m);
        var sameAccount = await CreateService().TransferAsync(user.Id, accounts[0].Id, accounts[0].AccountNumber, 10m);
        var tooMuch = await CreateService().TransferAsync(user.Id, accounts[0].Id, accounts[1].AccountNumber, 100.01m);
        var foreignSource = await CreateService().TransferAsync(user.Id, otherAccounts[0].Id, accounts[1].AccountNumber, 10m);

        unknownRecipient.ErrorMessage.Should().Be("Recipient account does not exist.");
        sameAccount.ErrorMessage.Should().Be("Accounts must be different.");
        tooMuch.ErrorMessage.Should().Be("Insufficient funds.");
        foreignSource.ErrorMessage.Should().Be("Account does not exist.");

        (await GetBalanceAsync(accounts[0].Id)).Should().Be(100m);
        (await GetBalanceAsync(accounts[1].Id)).Should().Be(0m);
        (await GetBalanceAsync(otherAccounts[0].Id)).Should().Be(100m);

        await using var db = CreateContext();
        var accountIds = accounts.Select(a => a.Id).ToList();
        (await db.Transactions.CountAsync(t => accountIds.Contains(t.AccountId) && t.TransactionType.StartsWith("TRANSFER"))).Should().Be(0);
        (await db.AuditEntries.CountAsync(a => a.UserId == user.Id && a.Action == AuditActions.Transfer)).Should().Be(0);
        (await AuditChainIsIntactAsync()).Should().BeTrue();
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

    private async Task<bool> AuditChainIsIntactAsync()
    {
        await using var db = CreateContext();

        var result = await new AuditService(db, TestAuditKey).VerifyChainAsync();

        return result.IsValid;
    }

    private async Task<decimal> GetBalanceAsync(Guid accountId)
    {
        await using var db = CreateContext();

        return await db.LedgerEntries
            .Where(l => l.AccountId == accountId)
            .SumAsync(l => l.Amount);
    }

    private async Task<(User User, List<Account> Accounts)> CreateCustomerAsync(params decimal[] startBalances)
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

        db.Users.Add(user);

        var accounts = new List<Account>();

        for (var i = 0; i < startBalances.Length; i++)
        {
            string accountNumber;
            do
            {
                accountNumber = $"NKM-{Random.Shared.Next(10000, 99999)}";
            }
            while (accounts.Any(a => a.AccountNumber == accountNumber)
                   || await db.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

            var account = new Account
            {
                UserId = userId,
                AccountNumber = accountNumber,
                AccountType = "CHECKING",
                Name = $"Konto {i + 1}"
            };

            db.Accounts.Add(account);
            accounts.Add(account);

            if (startBalances[i] > 0)
            {
                var deposit = new Transaction
                {
                    AccountId = account.Id,
                    Amount = startBalances[i],
                    TransactionType = "DEPOSIT",
                    Status = "COMPLETED",
                    CompletedAt = DateTime.UtcNow
                };

                db.Transactions.Add(deposit);
                db.LedgerEntries.Add(new LedgerEntry
                {
                    AccountId = account.Id,
                    TransactionId = deposit.Id,
                    Amount = startBalances[i],
                    EntryType = "DEPOSIT",
                    Description = "Test seed"
                });
            }
        }

        await db.SaveChangesAsync();
        _userIds.Add(userId);

        return (user, accounts);
    }
}