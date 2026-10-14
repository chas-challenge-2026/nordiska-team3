using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.HealthChecks;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenTheDatabaseIsReachable_ReturnsHealthy()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);

        var result = await new DatabaseHealthCheck(context).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTheDatabaseCannotBeReached_ReturnsUnhealthy()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2")
            .Options;
        using var context = new ApplicationDbContext(options);

        var result = await new DatabaseHealthCheck(context).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}