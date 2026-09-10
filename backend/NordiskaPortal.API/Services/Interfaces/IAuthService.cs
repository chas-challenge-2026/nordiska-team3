using NordiskaPortal.API.DTOs.Auth;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult?> LoginWithPinAsync(LoginPinRequestDto request);
    Task<MeResponseDto?> GetCurrentUserAsync(Guid userId);
    Task LogoutAsync(Guid userId);
}