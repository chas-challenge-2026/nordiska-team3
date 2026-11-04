using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NordiskaPortal.API.Extensions;

namespace NordiskaPortal.Tests;

public class HttpPipelineTests
{
    private static WebApplicationBuilder NewBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        return builder;
    }

    private static HttpClient ClientFor(WebApplication app) =>
        new() { BaseAddress = new Uri(app.Urls.First()) };

    [Theory]
    [InlineData(RateLimitPolicies.Auth, "AuthPerMinute")]
    [InlineData(RateLimitPolicies.Refresh, "RefreshPerMinute")]
    [InlineData(RateLimitPolicies.MoneyTransaction, "MoneyTransactionPerMinute")]
    [InlineData(RateLimitPolicies.TaxReport, "TaxReportPerMinute")]
    [InlineData(RateLimitPolicies.AccountLookup, "AccountLookupPerMinute")]
    public async Task Policy_returns_429_with_Retry_After_when_the_limit_is_exceeded(string policy, string settingName)
    {
        var builder = NewBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:" + settingName] = "1"
        });
        builder.Services.AddRateLimiting(builder.Configuration);

        await using var app = builder.Build();
        app.UseRateLimiter();
        app.MapPost("/limited", () => Results.Ok()).RequireRateLimiting(policy);
        await app.StartAsync();

        using var client = ClientFor(app);

        var first = await client.PostAsync("/limited", null);
        var second = await client.PostAsync("/limited", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        second.Headers.RetryAfter.Should().NotBeNull();
        (await second.Content.ReadAsStringAsync()).Should().Contain("Too many requests");
    }

    [Fact]
    public async Task Responses_carry_the_security_headers()
    {
        var builder = NewBuilder();

        await using var app = builder.Build();
        app.UseSecurityHeaders();
        app.MapGet("/ping", () => "ok");
        await app.StartAsync();

        using var client = ClientFor(app);

        var response = await client.GetAsync("/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().Equal("strict-origin-when-cross-origin");
        response.Headers.GetValues("Permissions-Policy").Should().Equal("camera=(), microphone=(), geolocation=()");
    }
}