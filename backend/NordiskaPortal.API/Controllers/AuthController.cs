using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [HttpPost("login-pin")]
    public async Task<IActionResult> LoginPin(LoginPinRequestDto request)
    {
        var result = await _authService.LoginWithPinAsync(request);

        if (result is null) return Unauthorized(new ErrorResponseDto("Invalid personal number or PIN."));

        Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = result.RefreshTokenExpiry
        });

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

        Response.Cookies.Delete("refreshToken");

        return NoContent();
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
};