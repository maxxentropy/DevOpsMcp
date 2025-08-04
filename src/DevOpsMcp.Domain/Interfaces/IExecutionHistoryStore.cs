using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Eagle;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for storing and retrieving Eagle script execution history
/// </summary>
public interface IExecutionHistoryStore
{
    /// <summary>
    /// Adds an execution record to the history
    /// </summary>
    Task AddExecutionAsync(ExecutionHistoryEntry entry);
    
    /// <summary>
    /// Gets execution history for a session
    /// </summary>
    Task<IReadOnlyList<ExecutionHistoryEntry>> GetSessionHistoryAsync(string sessionId);
    
    /// <summary>
    /// Gets recent execution history
    /// </summary>
    Task<IReadOnlyList<ExecutionHistoryEntry>> GetRecentHistoryAsync(int count = 10);
    
    /// <summary>
    /// Clears history for a session
    /// </summary>
    Task ClearSessionHistoryAsync(string sessionId);
    
    /// <summary>
    /// Gets execution statistics
    /// </summary>
    Task<ExecutionStatistics> GetStatisticsAsync();
}

/// <summary>
/// Represents statistics about script executions
/// </summary>
public class ExecutionStatistics
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public long TotalMemoryUsed { get; set; }
}