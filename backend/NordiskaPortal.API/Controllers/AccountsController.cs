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

    public AccountsController(ApplicationDbContext context)
    {
        _context = context;
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
            AccountType = request.AccountType
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(new { account.Id, account.UserId, account.AccountType, account.Status });
    }

    public record TransactionRequest(decimal Amount);

    private async Task<decimal> GetBalance(Guid accountId)
    {
        return await _context.LedgerEntries
            .Where(l => l.AccountId == accountId)
            .SumAsync(l => l.Amount);
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

        var currentBalance = await GetBalance(accountId);
        if (currentBalance < request.Amount)
        {
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
            EntryType = "Withdrawal",
            Description = "Withdrawal"
        };

        _context.Transactions.Add(transaction);
        _context.LedgerEntries.Add(ledgerEntry);
        await _context.SaveChangesAsync();

        return Ok(new { transactionId = transaction.Id, balance = await GetBalance(accountId) });
    }
}