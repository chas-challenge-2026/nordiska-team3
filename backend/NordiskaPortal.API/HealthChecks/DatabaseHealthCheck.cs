using Microsoft.Extensions.Diagnostics.HealthChecks;
using NordiskaPortal.API.Data;

namespace NordiskaPortal.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not reachable.", ex);
        }
    }
}