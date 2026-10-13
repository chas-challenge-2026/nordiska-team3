using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class AuthServiceRefreshTokenTests
{
    private const string Pin = "1234";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IPersonalNumberProtector> _protector = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly User _user;
    private int _issued;

    public AuthServiceRefreshTokenTests()
    {
        _protector.Setup(p => p.ComputeHash(It.IsAny<string>())).Returns("hash");
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access");
        _tokens.Setup(t => t.GenerateRefreshToken()).Returns(() => $"refresh-{++_issued}");

        _user = new User
        {
            Id = Guid.NewGuid(),
            PersonalNumber = "enc",
            FirstName = "Test",
            LastName = "Testsson",
            Email = "test@example.com",
            PinHash = BCrypt.Net.BCrypt.HashPassword(Pin, workFactor: 4)
        };
        _users.Setup(r => r.GetByPersonalNumberHashAsync("hash")).ReturnsAsync(_user);
        _users.Setup(r => r.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
    }

    [Fact]
    public async Task Login_StoresOnlyTheHashOfTheRefreshToken()
    {
        var login = await LoginAsync();

        login.RefreshToken.Should().Be("refresh-1");
        var row = _refreshTokens.Tokens.Should().ContainSingle().Subject;
        row.UserId.Should().Be(_user.Id);
        row.TokenHash.Should().Be(RefreshTokenHasher.Hash("refresh-1"));
        row.TokenHash.Should().NotContain("refresh-1");
        row.ExpiresAt.Should().Be(_clock.Now.UtcDateTime.AddDays(7));
        login.RefreshTokenExpiry.Should().Be(row.ExpiresAt);
        row.RotatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_RotatesTheToken()
    {
        await LoginAsync();

        var result = await CreateService().RefreshAsync("refresh-1");

        result.Should().NotBeNull();
        result!.RefreshToken.Should().Be("refresh-2");
        _refreshTokens.Tokens.Should().HaveCount(2);
        _refreshTokens.Tokens.Single(t => t.TokenHash == RefreshTokenHasher.Hash("refresh-1"))
            .RotatedAt.Should().Be(_clock.Now.UtcDateTime);
        _refreshTokens.Tokens.Single(t => t.TokenHash == RefreshTokenHasher.Hash("refresh-2"))
            .RotatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_AcceptsTheOldTokenAgainWithinTheGracePeriod()
    {
        await LoginAsync();
        await CreateService().RefreshAsync("refresh-1");
        _clock.Advance(TimeSpan.FromSeconds(10));

        var second = await CreateService().RefreshAsync("refresh-1");

        second.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_RejectsTheOldTokenAfterTheGracePeriod()
    {
        await LoginAsync();
        await CreateService().RefreshAsync("refresh-1");
        _clock.Advance(TimeSpan.FromSeconds(31));

        var second = await CreateService().RefreshAsync("refresh-1");

        second.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_RejectsAnExpiredToken()
    {
        await LoginAsync();
        _clock.Advance(TimeSpan.FromDays(7).Add(TimeSpan.FromSeconds(1)));

        var result = await CreateService().RefreshAsync("refresh-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_RejectsAnUnknownToken()
    {
        await LoginAsync();

        var result = await CreateService().RefreshAsync("never-issued");

        result.Should().BeNull();
        _refreshTokens.Tokens.Should().ContainSingle();
    }

    [Fact]
    public async Task EachDeviceGetsItsOwnToken()
    {
        await LoginAsync();
        await LoginAsync();

        _refreshTokens.Tokens.Should().HaveCount(2);
        (await CreateService().RefreshAsync("refresh-1")).Should().NotBeNull();
        (await CreateService().RefreshAsync("refresh-2")).Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_WithTheCookie_EndsOnlyThatDevice()
    {
        await LoginAsync();
        await LoginAsync();

        await CreateService().LogoutAsync(_user.Id, "refresh-1");

        (await CreateService().RefreshAsync("refresh-1")).Should().BeNull();
        (await CreateService().RefreshAsync("refresh-2")).Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_WithoutTheCookie_EndsEveryDevice()
    {
        await LoginAsync();
        await LoginAsync();

        await CreateService().LogoutAsync(_user.Id, null);

        _refreshTokens.Tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Logout_IgnoresATokenThatBelongsToSomeoneElse()
    {
        await LoginAsync();

        await CreateService().LogoutAsync(Guid.NewGuid(), "refresh-1");

        (await CreateService().RefreshAsync("refresh-1")).Should().NotBeNull();
    }

    [Fact]
    public async Task IssuingTokens_RemovesExpiredAndLongRotatedRows()
    {
        await LoginAsync();
        await CreateService().RefreshAsync("refresh-1");
        _clock.Advance(TimeSpan.FromDays(8));

        await LoginAsync();

        _refreshTokens.Tokens.Should().ContainSingle()
            .Which.TokenHash.Should().Be(RefreshTokenHasher.Hash("refresh-3"));
    }

    private async Task<AuthResult> LoginAsync()
    {
        var result = await CreateService().LoginWithPinAsync(new LoginPinRequestDto("19900101-1234", Pin));
        return result.AuthResult!;
    }

    private AuthService CreateService() =>
        new(_users.Object, _refreshTokens, _tokens.Object, _protector.Object,
            NullLogger<AuthService>.Instance, _clock, new ConfigurationBuilder().Build());
}