using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.API.DTOs.ErrorResponse;

namespace NordiskaPortal.API.Extensions;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Refresh = "refresh";
    public const string MoneyTransaction = "money-transaction";
    public const string TaxReport = "tax-report";
}

public static class RateLimitingExtensions
{
    private const int WindowSeconds = 60;

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");
        var authLimit = section.GetValue("AuthPerMinute", 10);
        var refreshLimit = section.GetValue("RefreshPerMinute", 30);
        var moneyLimit = section.GetValue("MoneyTransactionPerMinute", 10);
        var taxLimit = section.GetValue("TaxReportPerMinute", 5);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Inte inloggad: Ränkar per IP adress.
            options.AddPolicy(RateLimitPolicies.Auth, ctx => ByIp(ctx, authLimit));
            options.AddPolicy(RateLimitPolicies.Refresh, ctx => ByIp(ctx, refreshLimit));

            // Inloggad: Ränka per användare, så att kunder bakom samma IP adress inte delar budget.
            options.AddPolicy(RateLimitPolicies.MoneyTransaction, ctx => ByUser(ctx, moneyLimit));
            options.AddPolicy(RateLimitPolicies.TaxReport, ctx => ByUser(ctx, taxLimit));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;

                var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                    : WindowSeconds;
                http.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

                // Ingen persondata i loggen, bara enpoint och typ av partition.
                http.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting")
                    .LogWarning("Rate limit exceeded for {Method} {Path}", http.Request.Method, http.Request.Path);

                await http.Response.WriteAsJsonAsync(
                    new ErrorResponseDto("Too many requests. Please try again later."),
                    cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> ByIp(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetSlidingWindowLimiter(
            "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            _ => WindowOptions(permitsPerMinute));

    private static RateLimitPartition<string> ByUser(HttpContext context, int permitsPerMinute)
    {
        var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var key = userId is not null
            ? "user:" + userId
            : "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => WindowOptions(permitsPerMinute));
    }

    private static SlidingWindowRateLimiterOptions WindowOptions(int permitsPerMinute) => new()
    {
        PermitLimit = permitsPerMinute,
        Window = TimeSpan.FromSeconds(WindowSeconds),
        SegmentsPerWindow = 6,
        QueueLimit = 0,
        AutoReplenishment = true
    };
}