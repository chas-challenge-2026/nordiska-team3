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
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRepository<LedgerEntry> _ledgerEntryRepository;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly ApplicationDbContext _context; // Enbart för row-lock transaktionerna i "WithdrawAsync" och "TransferAsync"
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IUserRepository userRepository,
        ITransactionRepository transactionRepository,
        IRepository<LedgerEntry> ledgerEntryRepository,
        IRepository<Notification> notificationRepository,
        ApplicationDbContext context,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _notificationRepository = notificationRepository;
        _context = context;
        _logger = logger;
    }

    private static readonly Dictionary<string, string> DefaultAccountNames = new()
    {
        ["SAVINGS"] = "Sparkonto",
        ["CHECKING"] = "Transaktionskonto"
    };

    public async Task<AccountDto?> CreateAccountAsync(Guid userId, string accountType)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) return null;

        var account = new Account
        {
            UserId = userId,
            AccountType = accountType,
            Name = DefaultAccountNames.GetValueOrDefault(accountType, accountType),
            AccountNumber = await GenerateUniqueAccountNumberAsync()
        };

        await _accountRepository.AddAsync(account);
        await _accountRepository.SaveChangesAsync();

        _logger.LogInformation("Account {AccountId} created for user {UserId} with type {AccountType}",
            account.Id, account.UserId, account.AccountType);

        return MapToDto(account, balance: 0m);
    }

    public async Task<AccountDto?> RenameAccountAsync(Guid userId, Guid accountId, string name)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account is null || account.UserId != userId) return null;

        account.Name = name;
        account.UpdatedAt = DateTime.UtcNow;

        await _accountRepository.SaveChangesAsync();

        _logger.LogInformation("Account {AccountId} renamed to {Name} by user {UserId}",
            account.Id, account.Name, userId);

        var balance = await _accountRepository.GetBalanceAsync(accountId);
        return MapToDto(account, balance);
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

        var notification = new Notification
        {
            UserId = userId,
            Type = "DEPOSIT_COMPLETED",
            Message = $"Deposit of {amount:F2} completed on account {account.AccountNumber}.",
            Status = "PENDING"
        };

        await _notificationRepository.AddAsync(notification);
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

        var notification = new Notification
        {
            UserId = userId,
            Type = "WITHDRAWAL_COMPLETED",
            Message = $"Withdrawal of {amount:F2} completed on account {account.AccountNumber}.",
            Status = "PENDING"
        };

        await _notificationRepository.AddAsync(notification);
        await _transactionRepository.AddAsync(transaction);
        await _ledgerEntryRepository.AddAsync(ledgerEntry);
        await _accountRepository.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        var newBalance = await _accountRepository.GetBalanceAsync(accountId);

        _logger.LogInformation("Withdrawal of {Amount} completed on account {AccountId}, transaction {TransactionId}",
            amount, accountId, transaction.Id);

        return AccountOperationResult.Success(transaction.Id, newBalance);
    }

    // Slår upp mottagare via kontonummer. Returnerar bara maskerat namn ("Robin M."), aldrig personnummer eller e-post.
    public async Task<RecipientLookupDto?> LookupRecipientAsync(string accountNumber)
    {
        var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
        if (account is null) return null;

        var owner = await _userRepository.GetByIdAsync(account.UserId);
        if (owner is null) return null;

        return new RecipientLookupDto(account.AccountNumber, MaskName(owner.FirstName, owner.LastName));
    }

    // Atomisk överforing: båda kontona låst i fast ordning (lägst konto-Id först) så att två överföringar aldrig kan hamna i deadlock
    public async Task<TransferOperationResult> TransferAsync(Guid userId, Guid fromAccountId, string toAccountNumber, decimal amount)
    {
        if (amount <= 0) return TransferOperationResult.Failure("Amount must be positive.");

        var fromAccount = await _accountRepository.GetByIdAsync(fromAccountId);
        if (fromAccount is null || fromAccount.UserId != userId) return TransferOperationResult.Failure("Account does not exist.");

        var toAccount = await _accountRepository.GetByAccountNumberAsync(toAccountNumber);
        if (toAccount is null) return TransferOperationResult.Failure("Recipient account does not exist.");

        if (toAccount.Id == fromAccount.Id) return TransferOperationResult.Failure("Accounts must be different.");

        var isOwnTransfer = toAccount.UserId == userId;

        var sender = await _userRepository.GetByIdAsync(userId);
        var recipient = isOwnTransfer ? sender : await _userRepository.GetByIdAsync(toAccount.UserId);
        if (sender is null || recipient is null) return TransferOperationResult.Failure("Account does not exist.");

        // Etiketter som visas i historiken: eget konto visar kontonamn, annan användare visar maskerat namn
        var outLabel = isOwnTransfer ? toAccount.Name : MaskName(recipient.FirstName, recipient.LastName);
        var inLabel = isOwnTransfer ? fromAccount.Name : MaskName(sender.FirstName, sender.LastName);

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        foreach (var lockId in new[] { fromAccount.Id, toAccount.Id }.OrderBy(id => id))
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Accounts\" WHERE \"Id\" = {lockId} FOR UPDATE");
        }

        var currentBalance = await _accountRepository.GetBalanceAsync(fromAccount.Id);
        if (currentBalance < amount)
        {
            _logger.LogWarning("Transfer denied for account {AccountId}: insufficient funds (balance {Balance}, requested {Amount})",
                fromAccount.Id, currentBalance, amount);

            return TransferOperationResult.Failure("Insufficient funds.");
        }

        var transferId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow;

        var outTransaction = new Transaction
        {
            AccountId = fromAccount.Id,
            Amount = amount,
            TransactionType = "TRANSFER_OUT",
            Status = "COMPLETED",
            CompletedAt = completedAt,
            TransferId = transferId,
            CounterpartyLabel = outLabel
        };

        var inTransaction = new Transaction
        {
            AccountId = toAccount.Id,
            Amount = amount,
            TransactionType = "TRANSFER_IN",
            Status = "COMPLETED",
            CompletedAt = completedAt,
            TransferId = transferId,
            CounterpartyLabel = inLabel
        };

        var outLedgerEntry = new LedgerEntry
        {
            AccountId = fromAccount.Id,
            TransactionId = outTransaction.Id,
            Amount = -amount,
            EntryType = "TRANSFER_OUT",
            Description = $"Transfer to {toAccount.AccountNumber}"
        };

        var inLedgerEntry = new LedgerEntry
        {
            AccountId = toAccount.Id,
            TransactionId = inTransaction.Id,
            Amount = amount,
            EntryType = "TRANSFER_IN",
            Description = $"Transfer from {fromAccount.AccountNumber}"
        };

        await _notificationRepository.AddAsync(new Notification
        {
            UserId = userId,
            Type = "TRANSFER_SENT",
            Message = $"Transfer of {amount:F2} from account {fromAccount.AccountNumber} to {toAccount.AccountNumber} completed.",
            Status = "PENDING"
        });

        // Mottagaren får bara ett eget meddelande om kontot tillhör en annan användare
        if (!isOwnTransfer)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = toAccount.UserId,
                Type = "TRANSFER_RECEIVED",
                Message = $"You received {amount:F2} on account {toAccount.AccountNumber}.",
                Status = "PENDING"
            });
        }

        await _transactionRepository.AddAsync(outTransaction);
        await _transactionRepository.AddAsync(inTransaction);
        await _ledgerEntryRepository.AddAsync(outLedgerEntry);
        await _ledgerEntryRepository.AddAsync(inLedgerEntry);
        await _accountRepository.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        var fromBalance = await _accountRepository.GetBalanceAsync(fromAccount.Id);

        // Mottagarens saldo skickas bara tillbaka vid överforing mellan egna konton
        decimal? toBalance = isOwnTransfer ? await _accountRepository.GetBalanceAsync(toAccount.Id) : null;

        _logger.LogInformation("Transfer {TransferId} of {Amount} completed from account {FromAccountId} to account {ToAccountId}",
            transferId, amount, fromAccount.Id, toAccount.Id);

        return TransferOperationResult.Success(transferId, fromBalance, toBalance);
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

    public async Task<TransactionHistoryResponseDto?> GetTransactionHistoryAsync(Guid userId, Guid accountId, int page, int pageSize)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account is null || account.UserId != userId) return null;

        var (transactions, totalCount) = await _transactionRepository.GetByAccountIdAsync(accountId, page, pageSize);

        return BuildHistoryResponse(transactions, totalCount, page, pageSize);
    }

    // Alla konton för en användare i datumordning, med valfria filter. Null om accountId inte tillhör användaren.
    public async Task<TransactionHistoryResponseDto?> GetUserTransactionsAsync(
        Guid userId, Guid? accountId, string? type, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize)
    {
        if (accountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(accountId.Value);
            if (account is null || account.UserId != userId) return null;
        }

        // Datum tolkas som hela UTC-dagar, båda inkluderade
        DateTime? fromUtc = fromDate.HasValue
            ? DateTime.SpecifyKind(fromDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;

        DateTime? toUtcExclusive = toDate.HasValue && toDate.Value < DateOnly.MaxValue
            ? DateTime.SpecifyKind(toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;

        var (transactions, totalCount) = await _transactionRepository.GetByUserIdAsync(
            userId, accountId, type, fromUtc, toUtcExclusive, page, pageSize);

        return BuildHistoryResponse(transactions, totalCount, page, pageSize);
    }

    private static TransactionHistoryResponseDto BuildHistoryResponse(
        IReadOnlyList<Transaction> transactions, int totalCount, int page, int pageSize)
    {
        var dtos = transactions
            .Select(t => new TransactionDto(
                t.Id,
                t.AccountId,
                t.TransactionType,
                t.Amount.ToString("F2", CultureInfo.InvariantCulture),
                t.Status,
                t.CreatedAt,
                t.CompletedAt,
                t.TransferId,
                t.CounterpartyLabel))
            .ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new TransactionHistoryResponseDto(dtos, page, pageSize, totalCount, totalPages);
    }

    private static string MaskName(string firstName, string lastName)
    {
        var first = firstName.Trim();
        var last = lastName.Trim();
        return last.Length == 0 ? first : $"{first} {char.ToUpperInvariant(last[0])}.";
    }

    private static AccountDto MapToDto(Account account, decimal balance) =>
        new(
            account.Id,
            account.AccountNumber,
            account.AccountType,
            account.Name,
            account.Status,
            balance.ToString("F2", CultureInfo.InvariantCulture));
}

//
// Eventuellt lägga till "IUnitOfWork" för att istället för fyra separata repo parametrar.
//