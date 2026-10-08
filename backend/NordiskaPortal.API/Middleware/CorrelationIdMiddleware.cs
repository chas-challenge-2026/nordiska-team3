using Serilog.Context;

namespace NordiskaPortal.API.Middleware;

// Gives every request a correlation ID: reuses a valid X-Correlation-ID from the caller (or a proxy),
// otherwise creates one. The ID goes into the logs, the response header and HttpContext.TraceIdentifier.
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxLength = 64;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // The exception handler clears the response headers, so set the header again when the response starts.
        context.Response.OnStarting(() => { context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        return IsValid(incoming) ? incoming! : Guid.NewGuid().ToString("N");
    }

    // Only short, plain IDs are accepted, so a caller cannot put odd characters into the logs.
    private static bool IsValid(string? value) => !string.IsNullOrEmpty(value) && value.Length <= MaxLength && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
