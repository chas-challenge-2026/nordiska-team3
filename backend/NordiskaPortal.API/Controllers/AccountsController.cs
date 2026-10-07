using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.API.DTOs.Accounts;
using NordiskaPortal.API.DTOs.ErrorResponse;
using NordiskaPortal.API.Extensions;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class  AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // POST /api/accounts
    [HttpPost]
    public async Task<IActionResult> CreateAccount(CreateAccountRequestDto request)
    {
        var account = await _accountService.CreateAccountAsync(CurrentUserId, request.AccountType);

        if (account is null) return NotFound(new ErrorResponseDto("User not found."));

        return Ok(account);
    }

    // GET /api/accounts
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _accountService.GetAccountsForUserAsync(CurrentUserId);
        return Ok(accounts);
    }

    [HttpGet("{accountId}/balance")]
    public async Task<IActionResult> GetAccountBalance(Guid accountId)
    {
        var account = await _accountService.GetBalanceAsync(CurrentUserId, accountId);
        if (account is null) return NotFound(new ErrorResponseDto("Account does not exist."));

        return Ok(account);   
    }

    [EnableRateLimiting(RateLimitPolicies.MoneyTransaction)]
    [HttpPost("{accountId}/deposit")]
    public async Task<IActionResult> Deposit(Guid accountId, TransactionRequestDto request)
    {
        var result = await _accountService.DepositAsync(CurrentUserId, accountId, request.Amount);

        if (!result.IsSuccess) return BadRequest(new ErrorResponseDto(result.ErrorMessage!));

        return Ok(new TransactionResultDto(
            result.TransactionId!.Value,
            result.Balance!.Value.ToString("F2", CultureInfo.InvariantCulture)));
    }

    [EnableRateLimiting(RateLimitPolicies.MoneyTransaction)]
    [HttpPost("{accountId}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid accountId, TransactionRequestDto request)
    {
        var result = await _accountService.WithdrawAsync(CurrentUserId, accountId, request.Amount);

        if (!result.IsSuccess) return BadRequest(new ErrorResponseDto(result.ErrorMessage!));

        return Ok(new TransactionResultDto(
            result.TransactionId!.Value,
            result.Balance!.Value.ToString("F2", CultureInfo.InvariantCulture)));
    }

    [EnableRateLimiting(RateLimitPolicies.AccountLookup)]
    [HttpGet("lookup")]
    public async Task<IActionResult> LookupRecipient([FromQuery] string accountNumber)
    {
        var recipient = await _accountService.LookupRecipientAsync(accountNumber ?? string.Empty);

        if (recipient is null) return NotFound(new ErrorResponseDto("Account does not exist."));

        return Ok(recipient);
    }

    [EnableRateLimiting(RateLimitPolicies.MoneyTransaction)]
    [HttpPost("{accountId:guid}/transfer")]
    public async Task<IActionResult> Transfer(Guid accountId, TransferRequestDto request)
    {
        var result = await _accountService.TransferAsync(CurrentUserId, accountId, request.ToAccountNumber, request.Amount);

        if (!result.IsSuccess) return BadRequest(new ErrorResponseDto(result.ErrorMessage!));

        return Ok(new TransferResultDto(
            result.TransferId!.Value,
            result.FromBalance!.Value.ToString("F2", CultureInfo.InvariantCulture),
            result.ToBalance?.ToString("F2", CultureInfo.InvariantCulture)));
    }

    [HttpPatch("{accountId:guid}/name")]
    public async Task<IActionResult> RenameAccount(Guid accountId, RenameAccountRequestDto request)
    {
        var account = await _accountService.RenameAccountAsync(CurrentUserId, accountId, request.Name);

        if (account is null) return NotFound(new ErrorResponseDto("Account does not exist."));

        return Ok(account);
    }

    // GET /api/accounts/{accountId}/transactions
    [HttpGet("{accountId:guid}/transactions")]
    public async Task<IActionResult> GetTransactionHistory(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var history = await _accountService.GetTransactionHistoryAsync(CurrentUserId, accountId, page, pageSize);

        if (history is null) return NotFound(new ErrorResponseDto("Account does not exist."));

        return Ok(history);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}