using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// In-memory store for Eagle script execution history
/// Phase 1: Execution history tracking
/// </summary>
public sealed class ExecutionHistoryStore : IExecutionHistoryStore, IDisposable
{
    private readonly ILogger<ExecutionHistoryStore> _logger;
    private readonly ConcurrentDictionary<string, List<ExecutionHistoryEntry>> _sessionHistory;
    private readonly ConcurrentDictionary<string, ExecutionHistoryEntry> _executionById;
    private readonly LinkedList<ExecutionHistoryEntry> _globalHistory;
    private readonly ReaderWriterLockSlim _historyLock;
    private readonly int _maxHistorySize;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public ExecutionHistoryStore(ILogger<ExecutionHistoryStore> logger, int maxHistorySize = 1000)
    {
        _logger = logger;
        _maxHistorySize = maxHistorySize;
        _sessionHistory = new ConcurrentDictionary<string, List<ExecutionHistoryEntry>>();
        _executionById = new ConcurrentDictionary<string, ExecutionHistoryEntry>();
        _globalHistory = new LinkedList<ExecutionHistoryEntry>();
        _historyLock = new ReaderWriterLockSlim();
        
        // Cleanup old history every 5 minutes
        _cleanupTimer = new Timer(
            CleanupOldHistory,
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Adds an execution result to the history
    /// </summary>
    public Task AddExecutionAsync(ExecutionHistoryEntry entry)
    {
        return Task.Run(() =>
        {
            try
            {
                // Add to execution by ID lookup
                _executionById[entry.ExecutionId] = entry;
                
                // Add to global history
                _historyLock.EnterWriteLock();
                try
                {
                    _globalHistory.AddLast(entry);
                    
                    // Trim global history if it exceeds max size
                    while (_globalHistory.Count > _maxHistorySize)
                    {
                        var oldest = _globalHistory.First;
                        if (oldest != null)
                        {
                            _globalHistory.RemoveFirst();
                            _executionById.TryRemove(oldest.Value.ExecutionId, out _);
                        }
                    }
                }
                finally
                {
                    _historyLock.ExitWriteLock();
                }
                
                // Add to session history if session ID provided
                if (!string.IsNullOrEmpty(entry.SessionId))
                {
                    var sessionList = _sessionHistory.GetOrAdd(entry.SessionId, _ => new List<ExecutionHistoryEntry>());
                    lock (sessionList)
                    {
                        sessionList.Add(entry);
                        
                        // Keep only recent items per session
                        if (sessionList.Count > 100)
                        {
                            sessionList.RemoveAt(0);
                        }
                    }
                }
                
                _logger.LogDebug("Added execution {ExecutionId} to history", entry.ExecutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add execution to history");
            }
        });
    }

    /// <summary>
    /// Gets execution history for a session
    /// </summary>
    public Task<IReadOnlyList<ExecutionHistoryEntry>> GetSessionHistoryAsync(string sessionId)
    {
        return Task.Run(() =>
        {
            try
            {
                if (_sessionHistory.TryGetValue(sessionId, out var sessionList))
                {
                    lock (sessionList)
                    {
                        return (IReadOnlyList<ExecutionHistoryEntry>)sessionList.ToList();
                    }
                }
                else
                {
                    return (IReadOnlyList<ExecutionHistoryEntry>)Array.Empty<ExecutionHistoryEntry>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session history for {SessionId}", sessionId);
                return Array.Empty<ExecutionHistoryEntry>();
            }
        });
    }

    /// <summary>
    /// Gets recent execution history
    /// </summary>
    public Task<IReadOnlyList<ExecutionHistoryEntry>> GetRecentHistoryAsync(int count = 10)
    {
        return Task.Run(() =>
        {
            try
            {
                _historyLock.EnterReadLock();
                try
                {
                    var results = _globalHistory
                        .Reverse()
                        .Take(count)
                        .ToList();
                    
                    return (IReadOnlyList<ExecutionHistoryEntry>)results;
                }
                finally
                {
                    _historyLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent execution history");
                return Array.Empty<ExecutionHistoryEntry>();
            }
        });
    }

    /// <summary>
    /// Gets execution statistics
    /// </summary>
    public Task<ExecutionStatistics> GetStatisticsAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                List<ExecutionHistoryEntry> history;
                _historyLock.EnterReadLock();
                try
                {
                    history = _globalHistory.ToList();
                }
                finally
                {
                    _historyLock.ExitReadLock();
                }
                
                if (history.Count == 0)
                {
                    return new ExecutionStatistics();
                }
                
                var successful = history.Where(r => r.Success).ToList();
                var failed = history.Where(r => !r.Success).ToList();
                
                var avgMs = history.Average(r => r.ExecutionTime.TotalMilliseconds);
                var totalMemory = history.Sum(r => r.MemoryUsageBytes);
                
                return new ExecutionStatistics
                {
                    TotalExecutions = history.Count,
                    SuccessfulExecutions = successful.Count,
                    FailedExecutions = failed.Count,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(avgMs),
                    TotalMemoryUsed = totalMemory
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get execution statistics");
                return new ExecutionStatistics();
            }
        });
    }

    /// <summary>
    /// Clears history for a specific session
    /// </summary>
    public Task ClearSessionHistoryAsync(string sessionId)
    {
        return Task.Run(() =>
        {
            if (_sessionHistory.TryRemove(sessionId, out var sessionList))
            {
                lock (sessionList)
                {
                    sessionList.Clear();
                }
                _logger.LogInformation("Cleared history for session {SessionId}", sessionId);
            }
        });
    }

    private void CleanupOldHistory(object? state)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            
            _historyLock.EnterWriteLock();
            try
            {
                // Remove old entries from global history
                var toRemove = new List<LinkedListNode<ExecutionHistoryEntry>>();
                var node = _globalHistory.First;
                
                while (node != null)
                {
                    if (node.Value.EndTime < cutoffTime)
                    {
                        toRemove.Add(node);
                        _executionById.TryRemove(node.Value.ExecutionId, out _);
                    }
                    node = node.Next;
                }
                
                foreach (var removeNode in toRemove)
                {
                    _globalHistory.Remove(removeNode);
                }
                
                if (toRemove.Count > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old execution history entries", toRemove.Count);
                }
            }
            finally
            {
                _historyLock.ExitWriteLock();
            }
            
            // Clean up old sessions
            var oldSessions = _sessionHistory
                .Where(kvp => kvp.Value.Count == 0 || 
                            kvp.Value.All(r => r.EndTime < cutoffTime))
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var sessionId in oldSessions)
            {
                _sessionHistory.TryRemove(sessionId, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during history cleanup");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _cleanupTimer?.Dispose();
        _historyLock?.Dispose();
        _disposed = true;
    }
    
    /// <summary>
    /// Internal method for adding EagleExecutionResult (used by EagleScriptExecutor)
    /// </summary>
    internal Task AddExecutionResultAsync(EagleExecutionResult result, string? sessionId = null)
    {
        var entry = new ExecutionHistoryEntry
        {
            ExecutionId = result.ExecutionId,
            SessionId = sessionId ?? string.Empty,
            Script = string.Empty, // Script not stored in EagleExecutionResult
            Result = result.Result ?? string.Empty,
            Success = result.IsSuccess,
            StartTime = result.StartTimeUtc,
            EndTime = result.EndTimeUtc,
            ErrorMessage = result.ErrorMessage,
            ExecutionTime = result.Metrics?.ExecutionTime ?? TimeSpan.Zero,
            MemoryUsageBytes = result.Metrics?.MemoryUsageBytes ?? 0
        };
        
        return AddExecutionAsync(entry);
    }
}

