using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.DTOs.ErrorResponse;
using NordiskaPortal.API.Extensions;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private const string RefreshCookiePath = "/api/auth";

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register - Öppen för alla, ingen [Authorize] behövs
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.IsSuccess) return Conflict(new ErrorResponseDto(result.ErrorMessage!));

        SetRefreshTokenCookie(result.AuthResult!.RefreshToken, result.AuthResult.RefreshTokenExpiry);

        return StatusCode(StatusCodes.Status201Created, result.AuthResult.Response);
    }

    // POST /api/auth/login-pin - Öppen för alla, ingen [Authorize] behövs
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("login-pin")]
    public async Task<IActionResult> LoginPin(LoginPinRequestDto request)
    {
        var result = await _authService.LoginWithPinAsync(request);

        if (result.IsLockedOut)
        {
            var seconds = (int)Math.Ceiling(result.RetryAfter!.Value.TotalSeconds);
            Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status423Locked,
                new ErrorResponseDto("Too many failed PIN attempts. Try again later."));
        }

        if (!result.IsSuccess) return Unauthorized(new ErrorResponseDto("Invalid personal number or PIN."));

        SetRefreshTokenCookie(result.AuthResult!.RefreshToken, result.AuthResult.RefreshTokenExpiry);

        return Ok(result.AuthResult.Response);
    }

    // POST /api/auth/refresh
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new ErrorResponseDto("No refresh token provided."));

        var result = await _authService.RefreshAsync(refreshToken);

        if (result is null)
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = RefreshCookiePath });
            return Unauthorized(new ErrorResponseDto("Invalid or expired refresh token."));
        }

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);

        return Ok(result.Response);
    }

    // GET /api/auth/me - Kräver giltig token
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _authService.GetCurrentUserAsync(CurrentUserId);

        if (result is null) return NotFound(new ErrorResponseDto("User not found."));

        return Ok(result);
    }

    // POST /api/auth/logout - Kärver giltig token
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(CurrentUserId, Request.Cookies["refreshToken"]);

        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = RefreshCookiePath });

        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiry)
    {
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expiry
        });
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
};