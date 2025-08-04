using System;
using System.Collections.Generic;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for monitoring and tracking security events in Eagle scripts
/// </summary>
public interface IEagleSecurityMonitor
{
    /// <summary>
    /// Records a security event
    /// </summary>
    void RecordEvent(SecurityEvent securityEvent);
    
    /// <summary>
    /// Gets security events for a session
    /// </summary>
    IEnumerable<SecurityEvent> GetSessionEvents(string sessionId);
    
    /// <summary>
    /// Gets security metrics for a session
    /// </summary>
    SecurityMetrics GetSessionMetrics(string sessionId);
    
    /// <summary>
    /// Checks if a session has any violations
    /// </summary>
    bool HasViolations(string sessionId);
    
    /// <summary>
    /// Clears events for a session
    /// </summary>
    void ClearSessionEvents(string sessionId);
}

/// <summary>
/// Represents a security event
/// </summary>
public class SecurityEvent
{
    public string SessionId { get; set; } = string.Empty;
    public SecurityEventType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Types of security events
/// </summary>
public enum SecurityEventType
{
    CommandExecuted,
    AccessDenied,
    ResourceAccess,
    SecurityViolation,
    AuditEvent
}

/// <summary>
/// Security metrics for a session
/// </summary>
public class SecurityMetrics
{
    public int TotalEvents { get; set; }
    public int Violations { get; set; }
    public int DeniedOperations { get; set; }
    public Dictionary<SecurityEventType, int> EventCounts { get; init; } = new();
    public DateTimeOffset? FirstEvent { get; set; }
    public DateTimeOffset? LastEvent { get; set; }
}