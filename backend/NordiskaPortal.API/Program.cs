using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.ErrorResponse;
using NordiskaPortal.API.Extensions;
using NordiskaPortal.API.Filters;
using NordiskaPortal.API.HealthChecks;
using NordiskaPortal.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddExceptionHandling();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddSwaggerDocs();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var section = builder.Configuration.GetSection("ForwardedHeaders");

    foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? Array.Empty<string>())
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }

    foreach (var network in section.GetSection("KnownNetworks").Get<string[]>() ?? Array.Empty<string>())
    {
        var parts = network.Split('/');
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
            IPAddress.Parse(parts[0]), int.Parse(parts[1])));
    }
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSecurityHeaders();

app.UseMiddleware<CorrelationIdMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.ApplyMigrations();

await app.ProtectExistingPersonalNumbersAsync();

await app.VerifyAuditChainAsync();

if (app.Environment.IsDevelopment())
{
    await app.SeedTestDataAsync();
}

app.UseCors("Frontend");

app.UseHttpsRedirection();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health");

// Unknown API routes must give a 404, not the frontend's index.html.
app.MapFallback("/api/{**path}", () => Results.NotFound(new ErrorResponseDto("Not found.")));

app.MapFallbackToFile("index.html");

app.Run();