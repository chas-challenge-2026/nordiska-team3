using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResult?> LoginWithPinAsync(LoginPinRequestDto request)
    {
        var user = await _userRepository.GetByPersonalNumberAsync(request.PersonalNumber);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Pin, user.PinHash)) return null;

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpiry;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        var response = new LoginResponseDto(accessToken, MapToUserDto(user));

        return new AuthResult(response, refreshToken, refreshTokenExpiry);
    }

    public async Task<MeResponseDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null ? null : new MeResponseDto(MapToUserDto(user));
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }
    }

    private static UserDto MapToUserDto(User user) =>
        new(user.Id, $"{user.FirstName} {user.LastName}", user.Email, user.PersonalNumber);
}