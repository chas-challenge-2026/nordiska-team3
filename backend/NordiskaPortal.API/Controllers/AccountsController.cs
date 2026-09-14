using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.DTOs.Accounts;
using NordiskaPortal.API.DTOs.ErrorResponse;
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

    [HttpPost("{accountId}/deposit")]
    public async Task<IActionResult> Deposit(Guid accountId, TransactionRequestDto request)
    {
        var result = await _accountService.DepositAsync(CurrentUserId, accountId, request.Amount);

        if (!result.IsSuccess) return BadRequest(new ErrorResponseDto(result.ErrorMessage!));

        return Ok(new TransactionResultDto(
            result.TransactionId!.Value,
            result.Balance!.Value.ToString("F2", CultureInfo.InvariantCulture)));
    }

    [HttpPost("{accountId}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid accountId, TransactionRequestDto request)
    {
        var result = await _accountService.WithdrawAsync(CurrentUserId, accountId, request.Amount);

        if (!result.IsSuccess) return BadRequest(new ErrorResponseDto(result.ErrorMessage!));

        return Ok(new TransactionResultDto(
            result.TransactionId!.Value,
            result.Balance!.Value.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}