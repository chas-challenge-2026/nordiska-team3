using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class TokenServiceTests
{
    private const string JwtKey = "a-test-key-that-is-at-least-32-bytes-long!";

    private static TokenService CreateService(string key = JwtKey) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build());

    [Fact]
    public void GenerateAccessToken_PutsTheUserIdIssuerAndAudienceInTheToken()
    {
        var user = new User { Id = Guid.NewGuid() };

        var token = new JwtSecurityTokenHandler().ReadJwtToken(CreateService().GenerateAccessToken(user));

        token.Subject.Should().Be(user.Id.ToString());
        token.Issuer.Should().Be("TestIssuer");
        token.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAfterAboutFifteenMinutes()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(CreateService().GenerateAccessToken(new User { Id = Guid.NewGuid() }));

        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateAccessToken_IsSignedWithTheConfiguredKey()
    {
        var jwt = CreateService().GenerateAccessToken(new User { Id = Guid.NewGuid() });

        var rightKey = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey))
        };
        var wrongKey = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("another-key-that-is-also-32-bytes-long!!"))
        };
        var handler = new JwtSecurityTokenHandler();

        handler.ValidateToken(jwt, rightKey, out var validated);
        validated.Should().NotBeNull();

        var act = () => handler.ValidateToken(jwt, wrongKey, out _);
        act.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void GenerateRefreshToken_Returns64RandomBytesAsBase64()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Convert.FromBase64String(first).Should().HaveCount(64);
        second.Should().NotBe(first);
    }
}

public class RefreshTokenHasherTests
{
    [Fact]
    public void Hash_ReturnsTheSha256OfTheTokenAsUpperCaseHex()
    {
        RefreshTokenHasher.Hash("abc").Should().Be("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
    }

    [Fact]
    public void Hash_IsStableAndFitsTheTokenHashColumn()
    {
        var hash = RefreshTokenHasher.Hash("some-refresh-token");

        RefreshTokenHasher.Hash("some-refresh-token").Should().Be(hash);
        RefreshTokenHasher.Hash("some-other-token").Should().NotBe(hash);
        hash.Should().HaveLength(64);
    }
}

public class TaxReportQueueTests
{
    [Fact]
    public async Task DequeueAsync_ReturnsTheReportsInTheOrderTheyWereQueued()
    {
        var queue = new TaxReportQueue();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        queue.Enqueue(first);
        queue.Enqueue(second);

        (await queue.DequeueAsync(CancellationToken.None)).Should().Be(first);
        (await queue.DequeueAsync(CancellationToken.None)).Should().Be(second);
    }

    [Fact]
    public async Task DequeueAsync_WaitsUntilAReportIsQueued()
    {
        var queue = new TaxReportQueue();
        var id = Guid.NewGuid();

        var waiting = queue.DequeueAsync(CancellationToken.None);
        waiting.IsCompleted.Should().BeFalse();

        queue.Enqueue(id);

        (await waiting).Should().Be(id);
    }

    [Fact]
    public async Task DequeueAsync_WhenCancelled_Throws()
    {
        var queue = new TaxReportQueue();
        using var source = new CancellationTokenSource();

        var waiting = queue.DequeueAsync(source.Token);
        source.Cancel();

        await ((Func<Task>)(() => waiting)).Should().ThrowAsync<OperationCanceledException>();
    }
}

public class LoggingEmailSenderTests
{
    [Fact]
    public async Task SendAsync_CompletesWithoutSendingAnything()
    {
        var sender = new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance);

        var act = () => sender.SendAsync("anna@example.com", "Deposit", "hello");

        await act.Should().NotThrowAsync();
    }
}