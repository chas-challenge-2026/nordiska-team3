using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class  AccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(ApplicationDbContext context, ILogger<AccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public record CreateAccountRequest (Guid UserId, string AccountType);

    [HttpPost]
    public async Task<IActionResult> CreateAccount(CreateAccountRequest request)
    {
        var userExist = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        
        if (!userExist)
        {
            return NotFound("User does not exist.");
        }

        var account = new Account
        {
            UserId = request.UserId,
            AccountType = request.AccountType,
            AccountNumber = GenerateAccountNumber()
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Account {AccountId} created for user {UserId} with type {AccountType}",
            account.Id, account.UserId, account.AccountType);

        return Ok(new { account.Id, account.UserId, account.AccountType, account.Status });
    }

    public record TransactionRequest(decimal Amount);

    private async Task<decimal> GetBalance(Guid accountId)
    {
        return await _context.LedgerEntries
            .Where(l => l.AccountId == accountId)
            .SumAsync(l => l.Amount);
    }
    
    private static string GenerateAccountNumber()
    {
        return $"NKM-{Random.Shared.Next(10000, 99999)}";
    }

    [HttpGet("{accountId}/balance")]
    public async Task<IActionResult> GetAccountBalance(Guid accountId)
    {
        var account = await _context.Accounts.FindAsync(accountId);
        if (account is null)
        {
            return NotFound("Account does not exist.");
        }

        return Ok(new { account.Id, balance = await GetBalance(accountId) });
        
    }

    [HttpPost("{accountId}/deposit")]
    public async Task<IActionResult> Deposit(Guid accountId, TransactionRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be positive.");
        }

        var account = await _context.Accounts.FindAsync(accountId);
        if (account is null)
        {
            return NotFound("Account does not exist.");
        }

        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = request.Amount,
            TransactionType = "DEPOSIT",
            Status = "COMPLETED",
            CompletedAt = DateTime.UtcNow
        };

        var ledgerEntry = new LedgerEntry
        {
            AccountId = accountId,
            TransactionId = transaction.Id,
            Amount = request.Amount,
            EntryType = "DEPOSIT",
            Description = "Deposit"
        };

        _context.Transactions.Add(transaction);
        _context.LedgerEntries.Add(ledgerEntry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deposit of {Amount} completed on account {AccountId}, transaction {TransactionId}",
            request.Amount, accountId, transaction.Id);

        return Ok(new { transactionId = transaction.Id, balance = await GetBalance(accountId) });
    }

    [HttpPost("{accountId}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid accountId, TransactionRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be positive.");
        }

        var account = await _context.Accounts.FindAsync(accountId);
        if (account is null)
        {
            return NotFound("Account does not exist.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Accounts\" WHERE \"Id\" = {accountId} FOR UPDATE");

        var currentBalance = await GetBalance(accountId);
        if (currentBalance < request.Amount)
        {
            _logger.LogWarning("Withdrawal denied for account {AccountId}: insufficient funds (balance {Balance}, requested {Amount})", 
                accountId, currentBalance, request.Amount);

            return BadRequest("Insufficient funds.");
        }

        var transaction = new Transaction
        {
            AccountId = accountId,
            Amount = request.Amount,
            TransactionType = "WITHDRAWAL",
            Status = "COMPLETED",
            CompletedAt = DateTime.UtcNow
        };

        var ledgerEntry = new LedgerEntry
        {
            AccountId = accountId,
            TransactionId = transaction.Id,
            Amount = -request.Amount,
            EntryType = "WITHDRAWAL",
            Description = "Withdrawal"
        };

        _context.Transactions.Add(transaction);
        _context.LedgerEntries.Add(ledgerEntry);
        await _context.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        _logger.LogInformation("Withdrawal of {Amount} completed on account {AccountId}, transaction {TransactionId}", 
            request.Amount, accountId, transaction.Id);

        return Ok(new { transactionId = transaction.Id, balance = await GetBalance(accountId) });
    }
}