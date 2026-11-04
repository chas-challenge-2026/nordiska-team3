using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Repositories;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class AuthServicePinLockoutTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
    private const string Pin = "1234";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IPersonalNumberProtector> _protector = new();

    public AuthServicePinLockoutTests()
    {
        _protector.Setup(p => p.ComputeHash(It.IsAny<string>())).Returns("hash");
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access");
        _tokens.Setup(t => t.GenerateRefreshToken()).Returns("refresh");
    }

    [Fact]
    public async Task WrongPinBelowTheLimit_IsInvalidCredentialsAndCountsTheAttempt()
    {
        var user = StoreUser();
        _users.Setup(r => r.RegisterFailedPinAttemptAsync(user.Id)).ReturnsAsync(4);

        var result = await CreateService().LoginWithPinAsync(Login("9999"));

        result.IsSuccess.Should().BeFalse();
        result.IsLockedOut.Should().BeFalse();
        _users.Verify(r => r.RegisterFailedPinAttemptAsync(user.Id), Times.Once);
        _users.Verify(r => r.LockPinLoginAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task WrongPinAtTheLimit_LocksTheAccountForFiveMinutes()
    {
        var user = StoreUser();
        _users.Setup(r => r.RegisterFailedPinAttemptAsync(user.Id)).ReturnsAsync(5);

        var result = await CreateService().LoginWithPinAsync(Login("9999"));

        result.IsLockedOut.Should().BeTrue();
        result.RetryAfter.Should().Be(TimeSpan.FromMinutes(5));
        _users.Verify(r => r.LockPinLoginAsync(user.Id, Now.UtcDateTime.AddMinutes(5)), Times.Once);
    }

    [Fact]
    public async Task LockedAccount_RefusesEvenTheCorrectPinAndDoesNotCountIt()
    {
        var user = StoreUser(lockedUntil: Now.UtcDateTime.AddMinutes(3));

        var result = await CreateService().LoginWithPinAsync(Login(Pin));

        result.IsSuccess.Should().BeFalse();
        result.IsLockedOut.Should().BeTrue();
        result.RetryAfter.Should().Be(TimeSpan.FromMinutes(3));
        _users.Verify(r => r.RegisterFailedPinAttemptAsync(It.IsAny<Guid>()), Times.Never);
        _tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ExpiredLock_AllowsTheLoginAndClearsTheLockState()
    {
        var user = StoreUser(lockedUntil: Now.UtcDateTime.AddSeconds(-1), failedAttempts: 2);

        var result = await CreateService().LoginWithPinAsync(Login(Pin));

        result.IsSuccess.Should().BeTrue();
        user.FailedPinAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task CorrectPin_ResetsTheFailedAttemptCounter()
    {
        var user = StoreUser(failedAttempts: 3);

        var result = await CreateService().LoginWithPinAsync(Login(Pin));

        result.IsSuccess.Should().BeTrue();
        result.AuthResult!.RefreshToken.Should().Be("refresh");
        user.FailedPinAttempts.Should().Be(0);
    }

    [Fact]
    public async Task UnknownPersonalNumber_IsInvalidCredentialsAndNothingIsCounted()
    {
        _users.Setup(r => r.GetByPersonalNumberHashAsync("hash")).ReturnsAsync((User?)null);

        var result = await CreateService().LoginWithPinAsync(Login(Pin));

        result.IsSuccess.Should().BeFalse();
        result.IsLockedOut.Should().BeFalse();
        _users.Verify(r => r.RegisterFailedPinAttemptAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task TheLimitComesFromConfiguration()
    {
        var user = StoreUser();
        _users.Setup(r => r.RegisterFailedPinAttemptAsync(user.Id)).ReturnsAsync(3);

        var result = await CreateService(maxAttempts: 3, minutes: 10).LoginWithPinAsync(Login("9999"));

        result.IsLockedOut.Should().BeTrue();
        result.RetryAfter.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task SuccessfulLogin_PersistsTheResetInTheDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var userId = Guid.NewGuid();

        await using (var seed = new ApplicationDbContext(options))
        {
            seed.Users.Add(new User
            {
                Id = userId,
                PersonalNumber = "enc",
                PersonalNumberHash = "hash",
                FirstName = "Test",
                LastName = "Testsson",
                Email = "test@example.com",
                PinHash = BCrypt.Net.BCrypt.HashPassword(Pin, workFactor: 4),
                FailedPinAttempts = 3,
                LockedUntil = Now.UtcDateTime.AddSeconds(-1)
            });
            await seed.SaveChangesAsync();
        }

        await using (var context = new ApplicationDbContext(options))
        {
                        var service = new AuthService(new UserRepository(context), new RefreshTokenRepository(context), _tokens.Object, _protector.Object,
                NullLogger<AuthService>.Instance, new FixedTimeProvider(Now), new ConfigurationBuilder().Build());

            var result = await service.LoginWithPinAsync(Login(Pin));

            result.IsSuccess.Should().BeTrue();
        }

        await using (var verify = new ApplicationDbContext(options))
        {
            var stored = await verify.Users.SingleAsync(u => u.Id == userId);
            stored.FailedPinAttempts.Should().Be(0);
            stored.LockedUntil.Should().BeNull();
        }
    }

    private User StoreUser(DateTime? lockedUntil = null, int failedAttempts = 0)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            PersonalNumber = "enc",
            FirstName = "Test",
            LastName = "Testsson",
            Email = "test@example.com",
            PinHash = BCrypt.Net.BCrypt.HashPassword(Pin, workFactor: 4),
            LockedUntil = lockedUntil,
            FailedPinAttempts = failedAttempts
        };
        _users.Setup(r => r.GetByPersonalNumberHashAsync("hash")).ReturnsAsync(user);
        return user;
    }

    private AuthService CreateService(int maxAttempts = 5, int minutes = 5)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PinLockout:MaxFailedAttempts"] = maxAttempts.ToString(),
                ["PinLockout:Minutes"] = minutes.ToString()
            })
            .Build();

        return new AuthService(_users.Object, new FakeRefreshTokenRepository(), _tokens.Object, _protector.Object,
            NullLogger<AuthService>.Instance, new FixedTimeProvider(Now), configuration);
    }

    private static LoginPinRequestDto Login(string pin) => new("19900101-1234", pin);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}