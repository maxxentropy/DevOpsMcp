using System;
using System.Collections.Generic;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for persistent session storage for Eagle scripts
/// </summary>
public interface IEagleSessionStore : IDisposable
{
    /// <summary>
    /// Gets a value from the session store
    /// </summary>
    string GetValue(string sessionId, string key);
    
    /// <summary>
    /// Sets a value in the session store
    /// </summary>
    void SetValue(string sessionId, string key, string value);
    
    /// <summary>
    /// Deletes a specific key from the session
    /// </summary>
    void DeleteValue(string sessionId, string key);
    
    /// <summary>
    /// Lists all keys for a given session
    /// </summary>
    List<string> ListKeys(string sessionId);
    
    /// <summary>
    /// Clears all data for a given session
    /// </summary>
    void ClearSession(string sessionId);
    
    /// <summary>
    /// Clears all expired sessions
    /// </summary>
    void ClearExpiredSessions(TimeSpan expiration);
}