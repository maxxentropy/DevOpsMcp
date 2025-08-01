using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Protocols;

public sealed class SseProtocolHandler : IProtocolHandler
{
    private readonly IMessageHandler _messageHandler;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<SseProtocolHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string Name => "sse";

    public SseProtocolHandler(
        IMessageHandler messageHandler,
        IConnectionManager connectionManager,
        ILogger<SseProtocolHandler> logger)
    {
        _messageHandler = messageHandler;
        _connectionManager = connectionManager;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SSE protocol handler started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SSE protocol handler stopped");
        return Task.CompletedTask;
    }

    public async Task HandleConnectionAsync(HttpContext context)
    {
        var connectionId = Guid.NewGuid().ToString();
        _logger.LogInformation("New SSE connection: {ConnectionId}", connectionId);

        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");
        context.Response.Headers.Append("X-Accel-Buffering", "no");

        var sender = new SseMessageSender(context.Response, _jsonOptions, _logger);
        await _connectionManager.AddConnectionAsync(connectionId, sender);

        try
        {
            await context.Response.Body.FlushAsync();
            
            // Send initial ping
            await sender.SendEventAsync("ping", new { timestamp = DateTimeOffset.UtcNow }, context.RequestAborted);

            // Keep connection alive
            while (!context.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), context.RequestAborted);
                await sender.SendEventAsync("ping", new { timestamp = DateTimeOffset.UtcNow }, context.RequestAborted);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection {ConnectionId} cancelled", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE connection {ConnectionId}", connectionId);
        }
        finally
        {
            await _connectionManager.RemoveConnectionAsync(connectionId);
            _logger.LogInformation("SSE connection {ConnectionId} closed", connectionId);
        }
    }

    public async Task<IResult> HandleRequestAsync(HttpContext context, McpRequest request)
    {
        try
        {
            var response = await _messageHandler.HandleRequestAsync(request, context.RequestAborted);
            return Results.Json(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SSE request");
            var errorResponse = new McpResponse
            {
                Jsonrpc = "2.0",
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal error",
                    Data = ex.Message
                },
                Id = request.Id ?? null!
            };
            return Results.Json(errorResponse, _jsonOptions);
        }
    }
}

internal sealed class SseMessageSender : IMessageSender
{
    private readonly HttpResponse _response;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public SseMessageSender(HttpResponse response, JsonSerializerOptions jsonOptions, Microsoft.Extensions.Logging.ILogger logger)
    {
        _response = response;
        _jsonOptions = jsonOptions;
        _logger = logger;
    }

    public async Task SendResponseAsync(McpResponse response, CancellationToken cancellationToken = default)
    {
        await SendEventAsync("message", response, cancellationToken);
    }

    public async Task SendNotificationAsync(McpNotification notification, CancellationToken cancellationToken = default)
    {
        await SendEventAsync("notification", notification, cancellationToken);
    }

    public async Task SendEventAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var message = $"event: {eventType}\ndata: {json}\n\n";
            
            await _response.WriteAsync(message, cancellationToken);
            await _response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SSE message");
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }
}