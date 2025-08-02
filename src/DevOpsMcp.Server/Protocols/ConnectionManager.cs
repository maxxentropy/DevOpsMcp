using System.Collections.Concurrent;
using System.Threading.Channels;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Protocols;

public sealed class ConnectionManager(ILogger<ConnectionManager> logger) : IConnectionManager
{
    private readonly ConcurrentDictionary<string, IMessageSender> _connections = new();
    private readonly ConcurrentDictionary<string, SessionConnection> _sessionConnections = new();

    public Task AddConnectionAsync(string connectionId, IMessageSender sender)
    {
        if (_connections.TryAdd(connectionId, sender))
        {
            logger.LogInformation("Added connection {ConnectionId}", connectionId);
        }
        else
        {
            logger.LogWarning("Failed to add connection {ConnectionId} - already exists", connectionId);
        }
        
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out _))
        {
            logger.LogInformation("Removed connection {ConnectionId}", connectionId);
        }
        else
        {
            logger.LogWarning("Failed to remove connection {ConnectionId} - not found", connectionId);
        }
        
        return Task.CompletedTask;
    }

    public Task<IMessageSender?> GetConnectionAsync(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var sender);
        return Task.FromResult(sender);
    }

    public async Task BroadcastAsync(McpNotification notification, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        
        foreach (var (connectionId, sender) in _connections)
        {
            tasks.Add(SendNotificationSafelyAsync(connectionId, sender, notification, cancellationToken));
        }
        
        await Task.WhenAll(tasks);
    }

    private async Task SendNotificationSafelyAsync(
        string connectionId,
        IMessageSender sender,
        McpNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendNotificationAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending notification to connection {ConnectionId}", connectionId);
            await RemoveConnectionAsync(connectionId);
        }
    }

    public Task<IConnection> CreateSessionConnectionAsync(string sessionId)
    {
        var connection = new SessionConnection(sessionId);
        _sessionConnections.AddOrUpdate(sessionId, connection, (_, _) => connection);
        logger.LogInformation("Created session connection {SessionId}", sessionId);
        return Task.FromResult<IConnection>(connection);
    }

    public Task<IConnection?> GetSessionConnectionAsync(string sessionId)
    {
        _sessionConnections.TryGetValue(sessionId, out var connection);
        return Task.FromResult<IConnection?>(connection);
    }
}

/// <summary>
/// Represents a session-based connection with message queuing
/// </summary>
internal sealed class SessionConnection : IConnection
{
    private readonly Channel<object> _messageQueue;
    
    public SessionConnection(string sessionId)
    {
        SessionId = sessionId;
        _messageQueue = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }
    
    public string SessionId { get; }
    
    public bool HasPendingMessages => _messageQueue.Reader.Count > 0;
    
    public async Task<object?> DequeueMessageAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _messageQueue.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
    
    public async Task EnqueueMessageAsync(object message)
    {
        await _messageQueue.Writer.WriteAsync(message);
    }
}