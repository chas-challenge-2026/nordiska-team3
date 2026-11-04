using NordiskaPortal.API.DTOs.Auth;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginWithPinAsync(LoginPinRequestDto request);
    Task<AuthResult?> RefreshAsync(string refreshToken);
    Task<AuthResult?> LoginWithPersonalNumberAsync(string personalNumber);
    Task<RegisterResult> RegisterAsync(RegisterRequestDto request);
    Task<MeResponseDto?> GetCurrentUserAsync(Guid userId);
    Task LogoutAsync(Guid userId, string? refreshToken);
}