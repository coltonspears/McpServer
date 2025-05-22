// Tools/SqlTools.cs
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SqlToolsMcpServer.Services;

namespace SqlToolsMcpServer.Tools;

[McpServerToolType]
public static class SqlTools
{
    [McpServerTool, Description("Get missing index recommendations.")]
    public static async Task<string> GetMissingIndexes(ISqlServerMonitorService monitor)
    {
        var data = await monitor.GetMissingIndexesAsync();
        return JsonSerializer.Serialize(data);
    }

    [McpServerTool, Description("Get currently blocked queries.")]
    public static async Task<string> GetBlockedQueries(ISqlServerMonitorService monitor)
    {
        var data = await monitor.GetBlockedQueriesAsync();
        return JsonSerializer.Serialize(data);
    }
    
    [McpServerTool, Description("Fetch recent deadlock graph XML.")]
    public static async Task<string> GetDeadlockGraph(
        ISqlServerMonitorService m
    ) => await m.GetDeadlockGraphAsync();

    [McpServerTool, Description("Show index usage statistics.")]
    public static async Task<string> GetIndexUsageStats(
        ISqlServerMonitorService m
    ) => JsonSerializer.Serialize(await m.GetIndexUsageStatsAsync());

    [McpServerTool, Description("List top N expensive queries (default N=10).")]
    public static IAsyncEnumerable<TopQuery> GetTopQueries(
        ISqlServerMonitorService monitor,
        
        [Description("How many top queries to return")]
        int topN = 10,
        CancellationToken cancellationToken = default
    ) => monitor.GetTopQueriesAsync(topN, cancellationToken);

    [McpServerTool, Description("Summarize wait statistics.")]
    public static async Task<string> GetWaitStats(
        ISqlServerMonitorService m
    ) => JsonSerializer.Serialize(await m.GetWaitStatsAsync());
}
