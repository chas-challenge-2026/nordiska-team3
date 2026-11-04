using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class RefreshTokenPersistenceTests
{
    private const string Pin = "1234";

    private readonly DbContextOptions<ApplicationDbContext> _options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;

    private readonly TestClock _clock = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IPersonalNumberProtector> _protector = new();
    private readonly Guid _userId = Guid.NewGuid();
    private int _issued;

    public RefreshTokenPersistenceTests()
    {
        _protector.Setup(p => p.ComputeHash(It.IsAny<string>())).Returns("hash");
        _protector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns("19900101-1234");
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access");
        _tokens.Setup(t => t.GenerateRefreshToken()).Returns(() => $"refresh-{++_issued}");

        using var context = new ApplicationDbContext(_options);
        context.Users.Add(new User
        {
            Id = _userId,
            PersonalNumber = "enc",
            PersonalNumberHash = "hash",
            FirstName = "Test",
            LastName = "Testsson",
            Email = "persist@example.com",
            PinHash = BCrypt.Net.BCrypt.HashPassword(Pin, workFactor: 4)
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task LoginRefreshAndLogout_ArePersistedThroughTheRealRepositories()
    {
        var login = await WithService(s => s.LoginWithPinAsync(new LoginPinRequestDto("19900101-1234", Pin)));
        login.IsSuccess.Should().BeTrue();

        (await Rows()).Should().ContainSingle()
            .Which.TokenHash.Should().Be(RefreshTokenHasher.Hash("refresh-1"));

        var refreshed = await WithService(s => s.RefreshAsync("refresh-1"));
        refreshed!.RefreshToken.Should().Be("refresh-2");

        var rows = await Rows();
        rows.Should().HaveCount(2);
        rows.Single(r => r.TokenHash == RefreshTokenHasher.Hash("refresh-1")).RotatedAt.Should().NotBeNull();
        rows.Single(r => r.TokenHash == RefreshTokenHasher.Hash("refresh-2")).RotatedAt.Should().BeNull();

        _clock.Advance(TimeSpan.FromSeconds(1));
        
        await WithService(async s =>
        {
            await s.LogoutAsync(_userId, "refresh-2");
            return 0;
        });

        (await Rows()).Should().BeEmpty();
    }

    private async Task<T> WithService<T>(Func<AuthService, Task<T>> action)
    {
        await using var context = new ApplicationDbContext(_options);
        var service = new AuthService(new UserRepository(context), new RefreshTokenRepository(context),
            _tokens.Object, _protector.Object, NullLogger<AuthService>.Instance, _clock,
            new ConfigurationBuilder().Build());
        return await action(service);
    }

    private async Task<List<RefreshToken>> Rows()
    {
        await using var context = new ApplicationDbContext(_options);
        return await context.RefreshTokens.AsNoTracking().ToListAsync();
    }
}