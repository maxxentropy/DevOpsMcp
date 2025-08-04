using System.Collections.Generic;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for managing session state for a specific Eagle session
/// </summary>
public interface IEagleSessionManager
{
    /// <summary>
    /// Gets the session ID
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// Gets a value from the session
    /// </summary>
    string GetValue(string key);
    
    /// <summary>
    /// Sets a value in the session
    /// </summary>
    void SetValue(string key, string value);
    
    /// <summary>
    /// Deletes a key from the session
    /// </summary>
    void Delete(string key);
    
    /// <summary>
    /// Lists all keys in the session
    /// </summary>
    List<string> List();
    
    /// <summary>
    /// Clears all data in the session
    /// </summary>
    void Clear();
}