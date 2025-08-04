using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Monitors and tracks security metrics for Eagle script execution
/// Part of Phase 1: Execution sandbox security controls
/// </summary>
public class EagleSecurityMonitor : IEagleSecurityMonitor
{
    private readonly ILogger<EagleSecurityMonitor> _logger;
    private readonly ConcurrentDictionary<string, SecuritySessionMetrics> _sessions;
    private long _totalSecurityChecks;
    private long _blockedOperations;
    private long _allowedOperations;

    public EagleSecurityMonitor(ILogger<EagleSecurityMonitor> logger)
    {
        _logger = logger;
        _sessions = new ConcurrentDictionary<string, SecuritySessionMetrics>();
    }

    /// <summary>
    /// Records a security event
    /// </summary>
    public void RecordEvent(SecurityEvent securityEvent)
    {
        var session = _sessions.GetOrAdd(securityEvent.SessionId, _ => new SecuritySessionMetrics());
        session.RecordEvent(securityEvent);
        
        _logger.LogDebug("Recorded security event: {Type} for session {SessionId}", 
            securityEvent.Type, securityEvent.SessionId);
    }
    
    /// <summary>
    /// Gets security events for a session
    /// </summary>
    public IEnumerable<SecurityEvent> GetSessionEvents(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.GetEvents();
        }
        return Enumerable.Empty<SecurityEvent>();
    }
    
    /// <summary>
    /// Gets security metrics for a session
    /// </summary>
    public SecurityMetrics GetSessionMetrics(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.GetMetrics();
        }
        return new SecurityMetrics();
    }
    
    /// <summary>
    /// Checks if a session has any violations
    /// </summary>
    public bool HasViolations(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.HasViolations();
        }
        return false;
    }
    
    /// <summary>
    /// Clears events for a session
    /// </summary>
    public void ClearSessionEvents(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        _logger.LogInformation("Cleared security events for session {SessionId}", sessionId);
    }
    
    /// <summary>
    /// Records that a security check was performed
    /// </summary>
    public void RecordSecurityCheck(string sessionId, SecurityLevel level, string operation, bool allowed)
    {
        Interlocked.Increment(ref _totalSecurityChecks);
        
        if (allowed)
        {
            Interlocked.Increment(ref _allowedOperations);
        }
        else
        {
            Interlocked.Increment(ref _blockedOperations);
            _logger.LogWarning("Security blocked operation: {Operation} at level {Level}", operation, level);
        }

        var session = _sessions.GetOrAdd(sessionId, _ => new SecuritySessionMetrics());
        session.RecordCheck(operation, allowed);
    }



    /// <summary>
    /// Clears metrics for a session
    /// </summary>
    public void ClearSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Resets all metrics
    /// </summary>
    public void ResetAllMetrics()
    {
        _sessions.Clear();
        Interlocked.Exchange(ref _totalSecurityChecks, 0);
        Interlocked.Exchange(ref _blockedOperations, 0);
        Interlocked.Exchange(ref _allowedOperations, 0);
    }

    private Dictionary<string, int> GetMostUsedCommands()
    {
        var allOperations = new Dictionary<string, int>();
        
        foreach (var session in _sessions.Values)
        {
            foreach (var op in session.OperationCounts)
            {
                if (allOperations.ContainsKey(op.Key))
                {
                    allOperations[op.Key] += op.Value;
                }
                else
                {
                    allOperations[op.Key] = op.Value;
                }
            }
        }

        return allOperations
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}

/// <summary>
/// Metrics for a single security session
/// </summary>
public class SecuritySessionMetrics
{
    private readonly ConcurrentDictionary<string, int> _operationCounts;
    private readonly ConcurrentBag<SecurityEvent> _events;
    private long _totalChecks;
    private long _blockedChecks;
    private long _violations;

    public SecuritySessionMetrics()
    {
        _operationCounts = new ConcurrentDictionary<string, int>();
        _events = new ConcurrentBag<SecurityEvent>();
    }

    public int UniqueOperations => _operationCounts.Count;
    public long TotalChecks => Interlocked.Read(ref _totalChecks);
    public long BlockedChecks => Interlocked.Read(ref _blockedChecks);
    
    public void RecordEvent(SecurityEvent securityEvent)
    {
        _events.Add(securityEvent);
        Interlocked.Increment(ref _totalChecks);
        
        if (securityEvent.Type == SecurityEventType.SecurityViolation)
        {
            Interlocked.Increment(ref _violations);
        }
        else if (securityEvent.Type == SecurityEventType.AccessDenied)
        {
            Interlocked.Increment(ref _blockedChecks);
        }
        
        _operationCounts.AddOrUpdate(securityEvent.Type.ToString(), 1, (_, count) => count + 1);
    }

    public void RecordCheck(string operation, bool allowed)
    {
        Interlocked.Increment(ref _totalChecks);
        if (!allowed)
        {
            Interlocked.Increment(ref _blockedChecks);
        }
        
        _operationCounts.AddOrUpdate(operation, 1, (_, count) => count + 1);
    }
    
    public IEnumerable<SecurityEvent> GetEvents()
    {
        return _events.OrderBy(e => e.Timestamp);
    }
    
    public bool HasViolations()
    {
        return Interlocked.Read(ref _violations) > 0;
    }
    
    public SecurityMetrics GetMetrics()
    {
        var events = _events.ToList();
        var eventCounts = new Dictionary<SecurityEventType, int>();
        
        foreach (SecurityEventType eventType in Enum.GetValues<SecurityEventType>())
        {
            eventCounts[eventType] = events.Count(e => e.Type == eventType);
        }
        
        return new SecurityMetrics
        {
            TotalEvents = (int)TotalChecks,
            Violations = (int)Interlocked.Read(ref _violations),
            DeniedOperations = (int)BlockedChecks,
            EventCounts = eventCounts,
            FirstEvent = events.FirstOrDefault()?.Timestamp,
            LastEvent = events.LastOrDefault()?.Timestamp
        };
    }

    public IReadOnlyDictionary<string, int> OperationCounts => _operationCounts;
}

