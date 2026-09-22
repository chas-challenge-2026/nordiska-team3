using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.DTOs.ErrorResponse;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/login-pin - Öppen för alla, ingen [Authorize] behövs
    [EnableRateLimiting("SensitiveEndpointsPolicy")]
    [HttpPost("login-pin")]
    public async Task<IActionResult> LoginPin(LoginPinRequestDto request)
    {
        var result = await _authService.LoginWithPinAsync(request);

        if (result is null) return Unauthorized(new ErrorResponseDto("Invalid personal number or PIN."));

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);

        return Ok(result.Response);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new ErrorResponseDto("No refresh token provided."));

        var result = await _authService.RefreshAsync(refreshToken);

        if (result is null)
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/Auth" });
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
        await _authService.LogoutAsync(CurrentUserId);

        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/Auth" });

        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiry)
    {
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/Auth",
            Expires = expiry
        });
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}