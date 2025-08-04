using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DevOpsMcp.Infrastructure.Configuration;
using System.IO;
using DevOpsMcp.Domain.Interfaces;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// SQLite-based persistent session store for Eagle scripts
/// Maintains session state across interpreter instances and server restarts
/// </summary>
public sealed class EagleSessionStore : IEagleSessionStore
{
    private readonly ILogger<EagleSessionStore> _logger;
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;
    private bool _disposed;
    
    public EagleSessionStore(ILogger<EagleSessionStore> logger, IOptions<EagleOptions> options)
    {
        _logger = logger;
        
        // Use configured path or default to local app data
        var dbPath = options.Value.SessionStore?.DatabasePath ?? 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "DevOpsMcp", "eagle_sessions.db");
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        _connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
        
        InitializeDatabase();
        
        _logger.LogInformation("Initialized Eagle session store at {Path}", dbPath);
    }
    
    private void InitializeDatabase()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS sessions (
                session_id TEXT NOT NULL,
                key TEXT NOT NULL,
                value TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                PRIMARY KEY (session_id, key)
            );
            
            CREATE INDEX IF NOT EXISTS idx_session_id ON sessions(session_id);
            CREATE INDEX IF NOT EXISTS idx_updated_at ON sessions(updated_at);
        ";
        command.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Gets a value from the session
    /// </summary>
    public string GetValue(string sessionId, string key)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("GetValue called with empty sessionId");
            return string.Empty;
        }
        
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT value FROM sessions WHERE session_id = @sessionId AND key = @key";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            command.Parameters.AddWithValue("@key", key);
            
            var result = command.ExecuteScalar();
            if (result != null)
            {
                _logger.LogDebug("Retrieved value for session {SessionId}, key {Key}", sessionId, key);
                return result.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for session {SessionId}, key {Key}", sessionId, key);
        }
        
        _logger.LogDebug("No value found for session {SessionId}, key {Key}", sessionId, key);
        return string.Empty;
    }
    
    /// <summary>
    /// Sets a value in the session
    /// </summary>
    public void SetValue(string sessionId, string key, string value)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("SetValue called with empty sessionId");
            return;
        }
        
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO sessions (session_id, key, value, created_at, updated_at)
                VALUES (@sessionId, @key, @value, 
                    COALESCE((SELECT created_at FROM sessions WHERE session_id = @sessionId AND key = @key), datetime('now')),
                    datetime('now'))
            ";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);
            
            command.ExecuteNonQuery();
            _logger.LogDebug("Set value for session {SessionId}, key {Key}", sessionId, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for session {SessionId}, key {Key}", sessionId, key);
        }
    }
    
    /// <summary>
    /// Lists all keys in a session
    /// </summary>
    public List<string> ListKeys(string sessionId)
    {
        var keys = new List<string>();
        
        if (string.IsNullOrEmpty(sessionId))
        {
            return keys;
        }
        
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT key FROM sessions WHERE session_id = @sessionId ORDER BY key";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                keys.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing keys for session {SessionId}", sessionId);
        }
        
        return keys;
    }
    
    /// <summary>
    /// Clears all data for a session
    /// </summary>
    public void ClearSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return;
        }
        
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM sessions WHERE session_id = @sessionId";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            
            var deleted = command.ExecuteNonQuery();
            _logger.LogInformation("Cleared session {SessionId}, removed {Count} keys", sessionId, deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session {SessionId}", sessionId);
        }
    }
    
    /// <summary>
    /// Gets statistics about the session store
    /// </summary>
    public async Task<SessionStoreStatistics> GetStatisticsAsync()
    {
        var stats = new SessionStoreStatistics();
        
        try
        {
            using var command = _connection.CreateCommand();
            
            // Total sessions
            command.CommandText = "SELECT COUNT(DISTINCT session_id) FROM sessions";
            stats.TotalSessions = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            // Total keys
            command.CommandText = "SELECT COUNT(*) FROM sessions";
            stats.TotalKeys = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            // Database size
            command.CommandText = "SELECT page_count * page_size FROM pragma_page_count(), pragma_page_size()";
            stats.DatabaseSizeBytes = Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session store statistics");
        }
        
        return stats;
    }
    
    /// <summary>
    /// Cleans up old session data
    /// </summary>
    public async Task CleanupOldSessionsAsync(TimeSpan maxAge)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM sessions 
                WHERE datetime(updated_at) < datetime('now', @maxAge)
            ";
            command.Parameters.AddWithValue("@maxAge", $"-{maxAge.TotalSeconds} seconds");
            
            var deleted = await command.ExecuteNonQueryAsync();
            if (deleted > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old session entries", deleted);
                
                // Vacuum to reclaim space
                command.CommandText = "VACUUM";
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old sessions");
        }
    }
    
    /// <summary>
    /// Deletes a specific key from the session
    /// </summary>
    public void DeleteValue(string sessionId, string key)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("DeleteValue called with empty sessionId");
            return;
        }
        
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM sessions WHERE session_id = @sessionId AND key = @key";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            command.Parameters.AddWithValue("@key", key);
            
            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                _logger.LogDebug("Deleted value for session {SessionId}, key {Key}", sessionId, key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting value for session {SessionId}, key {Key}", sessionId, key);
        }
    }
    
    /// <summary>
    /// Clears all expired sessions
    /// </summary>
    public void ClearExpiredSessions(TimeSpan expiration)
    {
        try
        {
            var cutoffTime = DateTimeOffset.UtcNow.Subtract(expiration).ToString("O");
            
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM sessions WHERE updated_at < @cutoffTime";
            command.Parameters.AddWithValue("@cutoffTime", cutoffTime);
            
            var rowsAffected = command.ExecuteNonQuery();
            _logger.LogInformation("Cleared {Count} expired sessions older than {Expiration}", rowsAffected, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing expired sessions");
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing session store");
            }
        }
        
        _disposed = true;
    }
}

public class SessionStoreStatistics
{
    public int TotalSessions { get; set; }
    public int TotalKeys { get; set; }
    public long DatabaseSizeBytes { get; set; }
    
    public string DatabaseSizeMB => $"{DatabaseSizeBytes / 1024.0 / 1024.0:F2} MB";
}