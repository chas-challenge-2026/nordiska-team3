namespace NordiskaPortal.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.Data;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            // Check database connectivity
            var canConnectDb = await _dbContext.Database.CanConnectAsync();

            if (!canConnectDb)
            {
                _logger.LogWarning("Database health check failed: cannot connect");
                return StatusCode(503, new { status = "unhealthy", message = "Database unavailable" });
            }

            _logger.LogInformation("Health check passed");
            return Ok(new { status = "healthy", message = "Backend is up and database is accessible" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(500, new { status = "error", message = "Health check failed" });
        }
    }
}