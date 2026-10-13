using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class AuthService : IAuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan RefreshGracePeriod = TimeSpan.FromSeconds(30);

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IPersonalNumberProtector _personalNumberProtector;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxFailedPinAttempts;
    private readonly TimeSpan _pinLockoutDuration;

    public AuthService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService, IPersonalNumberProtector personalNumberProtector, ILogger<AuthService> logger,
        TimeProvider timeProvider, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
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
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var stored = await _refreshTokenRepository.GetByHashAsync(RefreshTokenHasher.Hash(refreshToken));

        if (stored is null || stored.ExpiresAt <= now)
        {
            _logger.LogWarning("Refresh token invalid or expired.");
            return null;
        }

        if (stored.RotatedAt is { } rotatedAt && now - rotatedAt > RefreshGracePeriod)
        {
            _logger.LogWarning("Refresh token for user {UserId} was used again after it was rotated.", stored.UserId);
            return null;
        }

        var user = await _userRepository.GetByIdAsync(stored.UserId);

        if (user is null)
        {
            _logger.LogWarning("Refresh token refers to a user that no longer exists.");
            return null;
        }

        stored.RotatedAt ??= now;

        var result = await IssueTokensAsync(user);

        _logger.LogInformation("Refreshed access token for user {UserId}.", user.Id);

        return result;
    }

    public async Task<MeResponseDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null ? null : new MeResponseDto(MapToUserDto(user));
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var toRemove = new List<RefreshToken>();

        if (string.IsNullOrEmpty(refreshToken))
        {
            toRemove.AddRange(await _refreshTokenRepository.GetByUserAsync(userId));
        }
        else
        {
            var stored = await _refreshTokenRepository.GetByHashAsync(RefreshTokenHasher.Hash(refreshToken));

            if (stored is not null && stored.UserId == userId)
                toRemove.Add(stored);
        }

        toRemove.AddRange(await _refreshTokenRepository.GetStaleAsync(userId, now, now));

        _refreshTokenRepository.RemoveRange(toRemove.DistinctBy(t => t.Id));
        await _refreshTokenRepository.SaveChangesAsync();

        _logger.LogInformation("User {UserId} logged out.", userId);
    }

    private UserDto MapToUserDto(User user) =>
        new(user.Id, $"{user.FirstName} {user.LastName}", user.Email,
            _personalNumberProtector.Unprotect(user.PersonalNumber));

    private async Task<AuthResult> IssueTokensAsync(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = now.Add(RefreshTokenLifetime);

        _refreshTokenRepository.RemoveRange(
            await _refreshTokenRepository.GetStaleAsync(user.Id, now, now - RefreshGracePeriod));

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = RefreshTokenHasher.Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshTokenExpiry
        });

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