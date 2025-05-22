using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace SqlToolsMcpServer.Services;
public interface ISqlServerMonitorService
{
    Task<string>                      GetDeadlockGraphAsync();
    Task<IEnumerable<IndexUsage>>     GetIndexUsageStatsAsync();
    IAsyncEnumerable<TopQuery> GetTopQueriesAsync(int topN, CancellationToken cancellationToken);
    Task<IEnumerable<WaitStat>>       GetWaitStatsAsync();
    Task<IEnumerable<MissingIndex>> GetMissingIndexesAsync();
    Task<IEnumerable<BlockedQuery>> GetBlockedQueriesAsync();
}

public class SqlServerMonitorService : ISqlServerMonitorService
{
    private readonly string _connectionString;
    public SqlServerMonitorService(IConfiguration config) =>
        _connectionString = config.GetConnectionString("DefaultConnection");

    public async Task<IEnumerable<MissingIndex>> GetMissingIndexesAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT 
  DB_NAME(mid.database_id) AS DatabaseName,
  OBJECT_NAME(mid.object_id, mid.database_id) AS TableName,
  mid.equality_columns,
  mid.inequality_columns,
  migs.avg_total_user_cost * migs.avg_user_impact 
    * (migs.user_seeks + migs.user_scans) AS IndexAdvantage
FROM sys.dm_db_missing_index_details AS mid
JOIN sys.dm_db_missing_index_groups AS mig
  ON mid.index_handle = mig.index_handle
JOIN sys.dm_db_missing_index_group_stats AS migs
  ON mig.index_group_handle = migs.group_handle
ORDER BY IndexAdvantage DESC;";
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<MissingIndex>();
        while (await reader.ReadAsync())
        {
            list.Add(new MissingIndex(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2), // Handle potential DBNull for EqualityColumns
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3), // Handle potential DBNull for InequalityColumns
                reader.GetDouble(4)
            ));
        }
        return list;
    }

    public async Task<IEnumerable<BlockedQuery>> GetBlockedQueriesAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT 
  blocking_session_id,
  session_id           AS blocked_session_id,
  wait_duration_ms,
  wait_type,
  resource_description
FROM sys.dm_os_waiting_tasks
WHERE blocking_session_id <> 0;";
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<BlockedQuery>();
        while (await reader.ReadAsync())
        {
            list.Add(new BlockedQuery(
                reader.GetInt16(0),
                reader.GetInt16(1),
                reader.GetInt64(2),
                reader.GetString(3),
                reader.GetString(4)
            ));
        }
        return list;
    }
    
    public async Task<string> GetDeadlockGraphAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT CAST(event_data AS XML) AS DeadlockGraph
FROM sys.fn_xe_file_target_read_file(
    'system_health*.xel', NULL, NULL, NULL
)
WHERE object_name = 'xml_deadlock_report';";
        var xml = await cmd.ExecuteScalarAsync();
        return xml?.ToString() ?? "<no-deadlock-found/>";
    }
    
    public async Task<IEnumerable<IndexUsage>> GetIndexUsageStatsAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT 
  DB_NAME(us.database_id)          AS DatabaseName,
  OBJECT_NAME(us.object_id, database_id) AS TableName,
  i.name                        AS IndexName,
  us.user_seeks,
  us.user_scans,
  us.user_lookups,
  us.user_updates
FROM sys.dm_db_index_usage_stats AS us
JOIN sys.indexes AS i
  ON us.object_id = i.object_id
  AND us.index_id  = i.index_id
WHERE us.database_id = DB_ID()
ORDER BY (us.user_seeks + us.user_scans) DESC;";
        
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<IndexUsage>();
        while (await reader.ReadAsync())
        {
            list.Add(new IndexUsage(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3),
                reader.GetInt64(4),
                reader.GetInt64(5),
                reader.GetInt64(6)
            ));
        }
        return list;
    }

    public async IAsyncEnumerable<TopQuery> GetTopQueriesAsync(
        int topN, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
SELECT TOP ({topN})
  qs.total_worker_time      AS TotalCpuTime,
  qs.execution_count        AS ExecCount,
  qs.total_logical_reads    AS TotalReads,
  SUBSTRING(
    qt.text, qs.statement_start_offset/2 + 1,
    (CASE WHEN qs.statement_end_offset = -1 
       THEN LEN(CONVERT(nvarchar(max), qt.text)) * 2 
       ELSE qs.statement_end_offset END
     - qs.statement_start_offset)/2 + 1
  )                         AS QueryText
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS qt
ORDER BY qs.total_worker_time DESC;";
        
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new TopQuery(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetString(3)
            );
        }
    }

    public async Task<IEnumerable<WaitStat>> GetWaitStatsAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT 
  wait_type,
  wait_time_ms,
  waiting_tasks_count,
  max_wait_time_ms
FROM sys.dm_os_wait_stats
WHERE waiting_tasks_count > 0
ORDER BY wait_time_ms DESC;";
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<WaitStat>();
        while (await reader.ReadAsync())
        {
            list.Add(new WaitStat (
                reader.GetString(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetInt64(3)
                ));
        }
        return list;
    }
}

public record IndexUsage(
    string DatabaseName,
    string TableName,
    string IndexName,
    long   UserSeeks,
    long   UserScans,
    long   UserLookups,
    long   UserUpdates
);

public record TopQuery(
    long   TotalCpuTime,
    long    ExecCount,
    long   TotalReads,
    string QueryText
);

public record WaitStat(
    string WaitType,
    long   WaitTimeMs,
    long    WaitingTasksCount,
    long   MaxWaitTimeMs
);

public record MissingIndex(string DatabaseName, string TableName, string EqualityColumns, string InequalityColumns, double IndexAdvantage);
public record BlockedQuery(int BlockingSessionId, int BlockedSessionId, long WaitDurationMs, string WaitType, string ResourceDescription);