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

// A login for a personal number that does not exist must take about as long as a login with a wrong PIN
// for one that does. Otherwise the response time shows which personal numbers have an account.
public class AuthServiceTimingTests
{
    // A real BCrypt check at the default work factor takes tens of milliseconds even on a fast machine.
    // Without it, an unknown personal number is answered in well under a millisecond.
    private static readonly TimeSpan AtLeast = TimeSpan.FromMilliseconds(20);

    [Fact]
    public async Task UnknownPersonalNumber_IsAnsweredAfterARealBcryptCheck()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByPersonalNumberHashAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

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

        var request = new LoginPinRequestDto("19900101-1234", "1234");

        await service.LoginWithPinAsync(request);

        var elapsed = new List<TimeSpan>();
        for (var i = 0; i < 3; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await service.LoginWithPinAsync(request);
            stopwatch.Stop();

            result.IsSuccess.Should().BeFalse();
            elapsed.Add(stopwatch.Elapsed);
        }

        elapsed.Min().Should().BeGreaterThan(AtLeast);
    }
}