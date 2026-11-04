using System.Diagnostics;
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

// A registration that is refused because the personal number or e-mail already exists must take about as
// long as a real registration. Otherwise the response time shows which personal numbers and e-mails exist.
public class AuthServiceRegisterTimingTests
{
    // A real BCrypt hash at the default work factor takes tens of milliseconds even on a fast machine.
    // Without it, a refused registration is answered in well under a millisecond.
    private static readonly TimeSpan AtLeast = TimeSpan.FromMilliseconds(20);

    [Fact]
    public async Task RegistrationWithAnExistingPersonalNumber_IsRefusedAfterABcryptHash()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByPersonalNumberHashAsync("hash")).ReturnsAsync(new User());

        await AssertRefusedAfterBcryptAsync(users);
    }

    [Fact]
    public async Task RegistrationWithAnExistingEmail_IsRefusedAfterABcryptHash()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByPersonalNumberHashAsync("hash")).ReturnsAsync((User?)null);
        users.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

        await AssertRefusedAfterBcryptAsync(users);
    }

    private static async Task AssertRefusedAfterBcryptAsync(Mock<IUserRepository> users)
    {
        var protector = new Mock<IPersonalNumberProtector>();
        protector.Setup(p => p.ComputeHash(It.IsAny<string>())).Returns("hash");

        var service = new AuthService(
            users.Object,
            new FakeRefreshTokenRepository(),
            new Mock<ITokenService>().Object,
            protector.Object,
            NullLogger<AuthService>.Instance,
            new TestClock(DateTimeOffset.UtcNow),
            new ConfigurationBuilder().Build());

        var request = new RegisterRequestDto("19900101-1234", "Test", "Testsson", "test@example.com", "1234");

        await service.RegisterAsync(request);

        var elapsed = new List<TimeSpan>();
        for (var i = 0; i < 3; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await service.RegisterAsync(request);
            stopwatch.Stop();

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Registration could not be completed with the provided details.");
            elapsed.Add(stopwatch.Elapsed);
        }

        elapsed.Min().Should().BeGreaterThan(AtLeast);
    }
}