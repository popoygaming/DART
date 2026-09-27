using DART.API.Net.Data;
using Microsoft.AspNetCore.Mvc;

namespace DART.API.Net.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly DartDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DartDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("Health check failed: database connectivity check returned false.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    database = "Unavailable"
                });
            }

            return Ok(new
            {
                status = "Healthy",
                database = "Connected"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed due to database connectivity exception.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                database = "Unavailable"
            });
        }
    }
}
