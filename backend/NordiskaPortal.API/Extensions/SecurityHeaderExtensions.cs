namespace NordiskaPortal.API.Extensions;

public static class SecurityHeadersExtensions
{
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        // HSTS är bara meningsfullt över HTTPS och ska inte gälla i lokal utveckling.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                return Task.CompletedTask;
            });

            await next();
        });

        return app;
    }
}