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
}

public interface IConnectionManager
{
    Task AddConnectionAsync(string connectionId, IMessageSender sender);
    Task RemoveConnectionAsync(string connectionId);
    Task<IMessageSender?> GetConnectionAsync(string connectionId);
    Task BroadcastAsync(McpNotification notification, CancellationToken cancellationToken = default);
}

public interface IMessageSender
{
    Task SendResponseAsync(McpResponse response, CancellationToken cancellationToken = default);
    Task SendNotificationAsync(McpNotification notification, CancellationToken cancellationToken = default);
}