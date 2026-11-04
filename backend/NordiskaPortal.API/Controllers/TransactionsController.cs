using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.DTOs.ErrorResponse;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "DEPOSIT", "WITHDRAWAL", "INTEREST", "TRANSFER_OUT", "TRANSFER_IN"
    };

    private readonly IAccountService _accountService;

    public TransactionsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // GET /api/transactions?page&pageSize&accountId&type&from&to
    // Användarens transaktioner från alla konton, nyast först. from/to är datum (yyyy-MM-dd), båda inkluderade (UTC-dagar).
    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? accountId,
        [FromQuery] string? type,
        [FromQuery(Name = "from")] DateOnly? fromDate,
        [FromQuery(Name = "to")] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var normalizedType = string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToUpperInvariant();
        if (normalizedType is not null && !AllowedTypes.Contains(normalizedType))
            return BadRequest(new ErrorResponseDto("Unknown transaction type."));

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            return BadRequest(new ErrorResponseDto("'from' must not be after 'to'."));

        var result = await _accountService.GetUserTransactionsAsync(
            CurrentUserId, accountId, normalizedType, fromDate, toDate, page, pageSize);

        if (result is null) return NotFound(new ErrorResponseDto("Account does not exist."));

        return Ok(result);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}