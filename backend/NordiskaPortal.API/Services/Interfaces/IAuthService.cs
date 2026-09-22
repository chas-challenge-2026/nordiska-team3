using NordiskaPortal.API.DTOs.Auth;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult?> LoginWithPinAsync(LoginPinRequestDto request);
    Task<AuthResult?> RefreshAsync(string refreshToken);
    Task<MeResponseDto?> GetCurrentUserAsync(Guid userId);
    Task LogoutAsync(Guid userId);
}