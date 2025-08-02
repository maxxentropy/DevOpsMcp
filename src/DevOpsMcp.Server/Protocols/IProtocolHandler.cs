using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Protocols;

public interface IProtocolHandler
{
    string Name { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public interface IMessageHandler
{
    Task<McpResponse> HandleRequestAsync(McpRequest request, CancellationToken cancellationToken = default);
    Task HandleNotificationAsync(McpNotification notification, CancellationToken cancellationToken = default);
    Task<McpResponse> ProcessRequestAsync(McpRequest request);
}

public interface IConnectionManager
{
    Task AddConnectionAsync(string connectionId, IMessageSender sender);
    Task RemoveConnectionAsync(string connectionId);
    Task<IMessageSender?> GetConnectionAsync(string connectionId);
    Task BroadcastAsync(McpNotification notification, CancellationToken cancellationToken = default);
    Task<IConnection> CreateSessionConnectionAsync(string sessionId);
    Task<IConnection?> GetSessionConnectionAsync(string sessionId);
}

public interface IConnection
{
    string SessionId { get; }
    bool HasPendingMessages { get; }
    Task<object?> DequeueMessageAsync(CancellationToken cancellationToken);
    Task EnqueueMessageAsync(object message);
}

public interface IMessageSender
{
    Task SendResponseAsync(McpResponse response, CancellationToken cancellationToken = default);
    Task SendNotificationAsync(McpNotification notification, CancellationToken cancellationToken = default);
}