using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IPersonalNumberProtector _personalNumberProtector;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxFailedPinAttempts;
    private readonly TimeSpan _pinLockoutDuration;

    public AuthService(IUserRepository userRepository, ITokenService tokenService,
        IPersonalNumberProtector personalNumberProtector, ILogger<AuthService> logger,
        TimeProvider timeProvider, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _personalNumberProtector = personalNumberProtector;
        _logger = logger;
        _timeProvider = timeProvider;
        _maxFailedPinAttempts = configuration.GetValue("PinLockout:MaxFailedAttempts", 5);
        _pinLockoutDuration = TimeSpan.FromMinutes(configuration.GetValue("PinLockout:Minutes", 5));
    }

    public async Task<LoginResult> LoginWithPinAsync(LoginPinRequestDto request)
    {
        var personalNumberHash = _personalNumberProtector.ComputeHash(request.PersonalNumber);
        var user = await _userRepository.GetByPersonalNumberHashAsync(personalNumberHash);

        if (user is null)
        {
            _logger.LogWarning("Failed PIN login attempt.");
            return LoginResult.InvalidCredentials();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            _logger.LogWarning("PIN login refused for user {UserId}: the account is locked.", user.Id);
            return LoginResult.LockedOut(lockedUntil - now);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Pin, user.PinHash))
        {
            var attempts = await _userRepository.RegisterFailedPinAttemptAsync(user.Id);

            if (attempts >= _maxFailedPinAttempts)
            {
                await _userRepository.LockPinLoginAsync(user.Id, now.Add(_pinLockoutDuration));
                _logger.LogWarning("User {UserId} locked after {Attempts} failed PIN attempts.", user.Id, attempts);
                return LoginResult.LockedOut(_pinLockoutDuration);
            }

            _logger.LogWarning("Failed PIN login attempt.");
            return LoginResult.InvalidCredentials();
        }

        user.FailedPinAttempts = 0;
        user.LockedUntil = null;

        var result = await IssueTokensAsync(user);

        _logger.LogInformation("User {UserId} logged in successfully.", user.Id);

        return LoginResult.Success(result);
    }

    public async Task<AuthResult?> RefreshAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);

        if (user is null || user.RefreshTokenExpiryTime is null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token invalid or expired.");
            return null;
        }

        var result = await IssueTokensAsync(user);

        _logger.LogInformation("Refreshed access token for user {UserId}.", user.Id);

        return result;
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

    private UserDto MapToUserDto(User user) =>
        new(user.Id, $"{user.FirstName} {user.LastName}", user.Email,
            _personalNumberProtector.Unprotect(user.PersonalNumber));

    private async Task<AuthResult> IssueTokensAsync(User user)
    {
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

    public async Task<RegisterResult> RegisterAsync(RegisterRequestDto request)
    {
        // Samma meddelande oavsett orsak, så klienten inte kan avgöra om ett personnummer eller e-post redan finns.
        const string genericConflictMessage =
            "Registration could not be completed with the provided details.";

        var personalNumberHash = _personalNumberProtector.ComputeHash(request.PersonalNumber);

        if (await _userRepository.GetByPersonalNumberHashAsync(personalNumberHash) is not null)
        {
            _logger.LogWarning("Registration attempt with already-registered personal number.");
            return RegisterResult.Failure(genericConflictMessage);
        }

        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            _logger.LogWarning("Registration attempt with already-registered email.");
            return RegisterResult.Failure(genericConflictMessage);
        }

        var user = new User
        {
            PersonalNumber = _personalNumberProtector.Protect(request.PersonalNumber),
            PersonalNumberHash = personalNumberHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin),
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var authResult = await IssueTokensAsync(user);

        _logger.LogInformation("User {UserId} registered successfully.", user.Id);

        return RegisterResult.Success(authResult);
    }
};