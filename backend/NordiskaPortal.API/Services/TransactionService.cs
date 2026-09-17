namespace NordiskaPortal.API.Services;

using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using Microsoft.EntityFrameworkCore;

public class TransactionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(ApplicationDbContext dbContext, ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Transaction> ProcessDepositAsync(Guid accountId, decimal amount, string description = "Deposit")
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Verify account exists
            var account = await _dbContext.Accounts.FindAsync(accountId);
            if (account == null)
                throw new InvalidOperationException("Account not found");

            // Create transaction record
            var txn = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                TransactionType = "DEPOSIT",
                Status = "COMPLETED",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            // Create ledger entry
            var ledgerEntry = new LedgerEntry
            {
                AccountId = accountId,
                TransactionId = txn.Id,
                Amount = amount,
                EntryType = "DEPOSIT",
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(txn);
            _dbContext.LedgerEntries.Add(ledgerEntry);

            // Create notification (queued, NOT sent directly)
            var notification = new Notification
            {
                UserId = account.UserId,
                Type = "DEPOSIT_COMPLETED",
                Message = $"Deposit of {amount} completed on account {account.AccountType}",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);

            // Save all together
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Deposit transaction {txn.Id} created with notification {notification.Id}");
            return txn;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Deposit transaction failed");
            throw;
        }
    }

    public async Task<Transaction> ProcessWithdrawalAsync(Guid accountId, decimal amount, string description = "Withdrawal")
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Verify account exists and has sufficient balance
            var account = await _dbContext.Accounts.FindAsync(accountId);
            if (account == null)
                throw new InvalidOperationException("Account not found");

            var balance = await _dbContext.LedgerEntries
                .Where(l => l.AccountId == accountId)
                .SumAsync(l => l.Amount);

            if (balance < amount)
                throw new InvalidOperationException("Insufficient funds");

            // Create transaction record
            var txn = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                TransactionType = "WITHDRAWAL",
                Status = "COMPLETED",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            // Create ledger entry (negative amount)
            var ledgerEntry = new LedgerEntry
            {
                AccountId = accountId,
                TransactionId = txn.Id,
                Amount = -amount,
                EntryType = "WITHDRAWAL",
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(txn);
            _dbContext.LedgerEntries.Add(ledgerEntry);

            // Create notification (queued, NOT sent directly)
            var notification = new Notification
            {
                UserId = account.UserId,
                Type = "WITHDRAWAL_COMPLETED",
                Message = $"Withdrawal of {amount} completed on account {account.AccountType}",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);

            // Save all together
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Withdrawal transaction {txn.Id} created with notification {notification.Id}");
            return txn;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Withdrawal transaction failed");
            throw;
        }
    }
}