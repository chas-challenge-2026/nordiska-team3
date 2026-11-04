using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.DTOs.ErrorResponse;
using NordiskaPortal.API.Extensions;
using NordiskaPortal.API.Services.BankId;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers;

// Login with BankID: start, then poll the status until the person has confirmed in the BankID app.
// No [Authorize], since the caller is not logged in yet (this is the login).
[ApiController]
[Route("api/auth/bankid")]
public class BankIdController : ControllerBase
{
    // Same cookie and path as in AuthController, so refresh and logout work the same after a BankID login.
    private const string RefreshCookiePath = "/api/auth";

    private readonly IBankIdService _bankIdService;

    public BankIdController(IBankIdService bankIdService)
    {
        _bankIdService = bankIdService;
    }

    // POST /api/auth/bankid/start
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("start")]
    public async Task<IActionResult> Start(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bankIdService.StartAsync(ClientIp(), cancellationToken);

            return Ok(new BankIdStartResponseDto(result.OrderId, result.AutoStartToken, _bankIdService.IsMock));
        }
        catch (BankIdApiException)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new ErrorResponseDto("BankID is not available right now."));
        }
    }

    // GET /api/auth/bankid/status/{orderId} - the frontend asks once per second
    [EnableRateLimiting(RateLimitPolicies.BankIdStatus)]
    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> Status(string orderId, CancellationToken cancellationToken)
    {
        var result = await _bankIdService.GetStatusAsync(orderId, cancellationToken);

        if (result.AuthResult is null)
        {
            return Ok(new BankIdStatusResponseDto(result.Status, result.HintCode, result.QrData, null));
        }

        SetRefreshTokenCookie(result.AuthResult.RefreshToken);

        return Ok(new BankIdStatusResponseDto(result.Status, null, null, result.AuthResult.Response));
    }

    // POST /api/auth/bankid/cancel/{orderId}
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("cancel/{orderId}")]
    public async Task<IActionResult> Cancel(string orderId, CancellationToken cancellationToken)
    {
        await _bankIdService.CancelAsync(orderId, cancellationToken);

        return NoContent();
    }

    // POST /api/auth/bankid/mock/approve - only exists in mock mode
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("mock/approve")]
    public IActionResult MockApprove(BankIdMockApproveRequestDto request)
    {
        if (!_bankIdService.IsMock || !_bankIdService.MockApprove(request.OrderId, request.PersonalNumber))
        {
            return NotFound();
        }

        return NoContent();
    }

    private string ClientIp()
    {
        var address = HttpContext.Connection.RemoteIpAddress;

        if (address is null)
        {
            return "127.0.0.1";
        }

        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });
    }
}