using NordiskaPortal.API.Data;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Extensions;

public static class DemoDataExtensions
{
    // Adds the demo customers and their history. Runs only when Demo:SeedData is true (any environment,
    // also Production, because the stage runs as Production). A failure is logged and never stops the app.
    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Demo:SeedData")) return;

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoDataSeeder>>();

        try
        {
            var seeder = new DemoDataSeeder(
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                scope.ServiceProvider.GetRequiredService<IPersonalNumberProtector>(),
                scope.ServiceProvider.GetRequiredService<TimeProvider>(),
                logger);

            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo data could not be added. The application starts without it.");
        }
    }
}