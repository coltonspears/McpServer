using Microsoft.AspNetCore.Mvc;
using SqlToolsMcpServer.Services;

namespace SqlToolsMcpServer.Controllers
{
    // This controller is for debugging purposes and should be secured or removed in production.
    // It provides endpoints to check the status of the SQL Server monitoring service and MCP server.
    // Ensure that you have proper authorization and authentication in place for production use.

    [ApiController]
    [Route("api/[controller]")] // Base route will be /api/McpDebug
    public class McpDebugController : ControllerBase
    {
        private readonly ISqlServerMonitorService _sqlMonitorService;
        private readonly ILogger<McpDebugController> _logger;
        private readonly IServiceProvider _serviceProvider; // Useful for resolving other services dynamically

        public McpDebugController(
            ISqlServerMonitorService sqlMonitorService,
            ILogger<McpDebugController> logger,
            IServiceProvider serviceProvider)
        {
            _sqlMonitorService = sqlMonitorService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [HttpGet("sql-blocked-queries")]
        public async Task<ActionResult> GetSqlBlockedQueries()
        {
            _logger.LogInformation("Endpoint 'sql-monitor-status' was called.");
            try
            {

                var blockedQueries = await _sqlMonitorService.GetBlockedQueriesAsync();
                return Ok(blockedQueries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SQL Monitor Service status.");
                return StatusCode(500, new { Error = "An error occurred while fetching SQL Monitor Service status.", Details = ex.Message });
            }
        }

        [HttpGet("deadlock-graph")]
        public async Task<ActionResult> GetDeadlockGraph()
        {
            _logger.LogInformation("Endpoint 'deadlock-graph' was called.");
            try
            {
                var deadlockGraph = await _sqlMonitorService.GetDeadlockGraphAsync();
                return Ok(deadlockGraph);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deadlock graph.");
                return StatusCode(500, new { Error = "An error occurred while fetching the deadlock graph.", Details = ex.Message });
            }
        }
        
        [HttpGet("index-usage-stats")]
        public async Task<ActionResult> GetIndexUsageStats()
        {
            _logger.LogInformation("Endpoint 'index-usage-stats' was called.");
            try
            {
                var indexUsageStats = await _sqlMonitorService.GetIndexUsageStatsAsync();
                return Ok(indexUsageStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving index usage stats.");
                return StatusCode(500, new { Error = "An error occurred while fetching index usage stats.", Details = ex.Message });
            }
        }
        
        [HttpGet("top-queries")]
        public ActionResult<IAsyncEnumerable<TopQuery>> GetTopQueries(int topN = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Endpoint 'top-queries' was called with topN={topN}.", topN);
            try
            {
                var topQueries = _sqlMonitorService.GetTopQueriesAsync(topN, cancellationToken);
                return Ok(topQueries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top queries.");
                return StatusCode(500, new { Error = "An error occurred while fetching top queries.", Details = ex.Message });
            }
        }
        
        [HttpGet("wait-stats")]
        public async Task<ActionResult> GetWaitStats()
        {
            _logger.LogInformation("Endpoint 'wait-stats' was called.");
            try
            {
                var waitStats = await _sqlMonitorService.GetWaitStatsAsync();
                return Ok(waitStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wait stats.");
                return StatusCode(500, new { Error = "An error occurred while fetching wait stats.", Details = ex.Message });
            }
        }
        
        
        
    }

}