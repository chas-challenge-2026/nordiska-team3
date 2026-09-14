using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.Accounts;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<LedgerEntry> _ledgerEntryRepository;
    private readonly ApplicationDbContext _context; // Enbart för row-lock transaktionen i "WithdrawAsync"
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IUserRepository userRepository,
        IRepository<Transaction> transactionRepository,
        IRepository<LedgerEntry> ledgerEntryRepository,
        ApplicationDbContext context,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<AccountDto?> CreateAccountAsync(Guid userId, string accountType)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) return null;

        var account = new Account
        {
            UserId = userId,
            AccountType = accountType,
            AccountNumber = await GenerateUniqueAccountNumberAsync()
        };

        await _accountRepository.AddAsync(account);
        await _accountRepository.SaveChangesAsync();

        _logger.LogInformation("Account {AccountId} created for user {UserId} with type {AccountType}",
            account.Id, account.UserId, account.AccountType);

        return MapToDto(account, balance: 0m);
    }

    public async Task<AccountsResponseDto> GetAccountsForUserAsync(Guid userId)
    {
        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        var dtos = new List<AccountDto>();

        foreach (var account in accounts)
        {
            var balance = await _accountRepository.GetBalanceAsync(account.Id);
            dtos.Add(MapToDto(account, balance));
        }

        return new AccountsResponseDto(dtos);
    }

    public async Task<AccountDto?> GetBalanceAsync(Guid userId, Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account is null || account.UserId != userId) return null;

        var balance = await _accountRepository.GetBalanceAsync(accountId);
        return MapToDto(account, balance);
    }

    public async Task<AccountOperationResult> DepositAsync(Guid userId, Guid accountId, decimal amount)
    {
        if (amount <= 0) return AccountOperationResult.Failure("Amount must be positive.");

        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account is null || account.UserId != userId) return AccountOperationResult.Failure("Account does not exist.");

        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            TransactionType = "DEPOSIT",
            Status = "COMPLETED",
            CompletedAt = DateTime.UtcNow
        };

        var ledgerEntry = new LedgerEntry
        {
            AccountId = accountId,
            TransactionId = transaction.Id,
            Amount = amount,
            EntryType = "DEPOSIT",
            Description = "Deposit"
        };

        await _transactionRepository.AddAsync(transaction);
        await _ledgerEntryRepository.AddAsync(ledgerEntry);
        await _accountRepository.SaveChangesAsync();

        var newBalance = await _accountRepository.GetBalanceAsync(accountId);

        _logger.LogInformation("Deposit of {Amount} completed on account {AccountId}, transaction {TransactionId}",
            amount, accountId, transaction.Id);

        return AccountOperationResult.Success(transaction.Id, newBalance);
    }

    public async Task<AccountOperationResult> WithdrawAsync(Guid userId, Guid accountId, decimal amount)
    {
        if (amount <= 0) return AccountOperationResult.Failure("Amount must be positive.");

        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account is null || account.UserId != userId) return AccountOperationResult.Failure("Account does not exist.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Accounts\" WHERE \"Id\" = {accountId} FOR UPDATE");

        var currentBalance = await _accountRepository.GetBalanceAsync(accountId);
        if (currentBalance < amount)
        {
            _logger.LogWarning("Withdrawal denied for account {AccountId}: insufficient funds (balance {Balance}, requested {Amount})",
                accountId, currentBalance, amount);

            return AccountOperationResult.Failure("Insufficient funds.");
        }

        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            TransactionType = "WITHDRAWAL",
            Status = "COMPLETED",
            CompletedAt = DateTime.UtcNow
        };

        var ledgerEntry = new LedgerEntry
        {
            AccountId = accountId,
            TransactionId = transaction.Id,
            Amount = -amount,
            EntryType = "WITHDRAWAL",
            Description = "Withdrawal"
        };

        await _transactionRepository.AddAsync(transaction);
        await _ledgerEntryRepository.AddAsync(ledgerEntry);
        await _accountRepository.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        var newBalance = await _accountRepository.GetBalanceAsync(accountId);

        _logger.LogInformation("Withdrawal of {Amount} completed on account {AccountId}, transaction {TransactionId}",
            amount, accountId, transaction.Id);

        return AccountOperationResult.Success(transaction.Id, newBalance);
    }

    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = $"NKM-{Random.Shared.Next(10000, 99999)}";
            if (!await _accountRepository.ExistsByAccountNumberAsync(candidate)) return candidate;
        }

        throw new InvalidOperationException("Could not generate a unique account number after several attempts.");
    }

    private static AccountDto MapToDto(Account account, decimal balance) =>
        new(
            account.Id,
            account.AccountNumber,
            account.AccountType,
            account.Status,
            balance.ToString("F2", CultureInfo.InvariantCulture));
}

//
// Eventuellt lägga till "IUnitOfWork" för att istället för fyra separata repo parametrar.
//