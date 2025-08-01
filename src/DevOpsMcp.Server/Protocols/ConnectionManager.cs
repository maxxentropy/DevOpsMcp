using System.Collections.Concurrent;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Protocols;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, IMessageSender> _connections = new();
    private readonly ILogger<ConnectionManager> _logger;

    public ConnectionManager(ILogger<ConnectionManager> logger)
    {
        _logger = logger;
    }

    public Task AddConnectionAsync(string connectionId, IMessageSender sender)
    {
        if (_connections.TryAdd(connectionId, sender))
        {
            _logger.LogInformation("Added connection {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogWarning("Failed to add connection {ConnectionId} - already exists", connectionId);
        }
        
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out _))
        {
            _logger.LogInformation("Removed connection {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogWarning("Failed to remove connection {ConnectionId} - not found", connectionId);
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
            _logger.LogError(ex, "Error sending notification to connection {ConnectionId}", connectionId);
            await RemoveConnectionAsync(connectionId);
        }
    }
}