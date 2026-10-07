using NordiskaPortal.API.DTOs.Auth;

namespace NordiskaPortal.API.Services;

public sealed record AuthResult(LoginResponseDto Response, string RefreshToken, DateTime RefreshTokenExpiry);