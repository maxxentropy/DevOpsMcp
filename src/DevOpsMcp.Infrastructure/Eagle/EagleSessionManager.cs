using System.Collections.Generic;
using DevOpsMcp.Domain.Interfaces;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Manages session state for a specific Eagle session
/// </summary>
public class EagleSessionManager : IEagleSessionManager
{
    private readonly IEagleSessionStore _store;
    private readonly string _sessionId;
    
    public EagleSessionManager(IEagleSessionStore store, string sessionId)
    {
        _store = store;
        _sessionId = sessionId;
    }
    
    public string SessionId => _sessionId;
    
    public string GetValue(string key)
    {
        return _store.GetValue(_sessionId, key);
    }
    
    public void SetValue(string key, string value)
    {
        _store.SetValue(_sessionId, key, value);
    }
    
    public void Delete(string key)
    {
        _store.DeleteValue(_sessionId, key);
    }
    
    public List<string> List()
    {
        return _store.ListKeys(_sessionId);
    }
    
    public void Clear()
    {
        _store.ClearSession(_sessionId);
    }
}