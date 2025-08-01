using DevOpsMcp.Server.Protocols;
using DevOpsMcp.Server.Tools;

namespace DevOpsMcp.Server.Mcp;

public sealed class MessageHandler : IMessageHandler
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<MessageHandler> _logger;
    private readonly ServerInfo _serverInfo = new()
    {
        Name = "DevOps MCP Server",
        Version = "1.0.0"
    };

    public MessageHandler(
        IToolRegistry toolRegistry,
        ILogger<MessageHandler> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<McpResponse> HandleRequestAsync(McpRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Handling request: {Method}", request.Method);

        try
        {
            var result = request.Method switch
            {
                "initialize" => await HandleInitializeAsync(request),
                "initialized" => HandleInitialized(),
                "tools/list" => await HandleListToolsAsync(),
                "tools/call" => await HandleCallToolAsync(request, cancellationToken),
                "ping" => HandlePing(),
                _ => throw new NotSupportedException($"Method {request.Method} is not supported")
            };

            return new McpResponse
            {
                Jsonrpc = "2.0",
                Result = result,
                Id = request.Id ?? throw new InvalidOperationException("Request ID is required")
            };
        }
        catch (NotSupportedException ex)
        {
            return CreateErrorResponse(request.Id!, -32601, "Method not found", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request {Method}", request.Method);
            return CreateErrorResponse(request.Id!, -32603, "Internal error", ex.Message);
        }
    }

    public Task HandleNotificationAsync(McpNotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Handling notification: {Method}", notification.Method);

        return notification.Method switch
        {
            "cancelled" => HandleCancelledNotification(notification),
            "progress" => HandleProgressNotification(notification),
            _ => Task.CompletedTask
        };
    }

    private Task<object> HandleInitializeAsync(McpRequest request)
    {
        var initRequest = request.Params?.Deserialize<InitializeRequest>() 
            ?? throw new ArgumentException("Invalid initialize request");

        _logger.LogInformation("Initializing with protocol version {Version}", initRequest.ProtocolVersion);

        var response = new InitializeResponse
        {
            ProtocolVersion = "2024-11-05",
            ServerInfo = _serverInfo,
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = true },
                Logging = new LoggingCapability 
                { 
                    Levels = new List<string> { "debug", "info", "warning", "error" } 
                }
            }
        };

        return Task.FromResult<object>(response);
    }

    private object HandleInitialized()
    {
        _logger.LogInformation("Server initialized");
        return new { };
    }

    private async Task<object> HandleListToolsAsync()
    {
        var tools = await _toolRegistry.GetToolsAsync();
        return new ListToolsResponse { Tools = tools };
    }

    private async Task<object> HandleCallToolAsync(McpRequest request, CancellationToken cancellationToken)
    {
        var callRequest = request.Params?.Deserialize<CallToolRequest>() 
            ?? throw new ArgumentException("Invalid tool call request");

        _logger.LogInformation("Calling tool: {ToolName}", callRequest.Name);

        var result = await _toolRegistry.CallToolAsync(
            callRequest.Name, 
            callRequest.Arguments, 
            cancellationToken);

        return new CallToolResponse
        {
            Content = result.Content,
            IsError = result.IsError
        };
    }

    private object HandlePing()
    {
        return new { pong = true, timestamp = DateTimeOffset.UtcNow };
    }

    private Task HandleCancelledNotification(McpNotification notification)
    {
        var requestId = (notification.Params as JsonElement?)?.GetProperty("requestId").GetString();
        _logger.LogInformation("Request {RequestId} was cancelled", requestId);
        return Task.CompletedTask;
    }

    private Task HandleProgressNotification(McpNotification notification)
    {
        var progress = notification.Params != null ? JsonSerializer.Deserialize<ProgressNotification>(notification.Params.ToString()) : null;
        if (progress != null)
        {
            _logger.LogDebug("Progress for {Token}: {Progress}/{Total}", 
                progress.ProgressToken, progress.Progress, progress.Total);
        }
        return Task.CompletedTask;
    }

    private McpResponse CreateErrorResponse(object id, int code, string message, object? data = null)
    {
        return new McpResponse
        {
            Jsonrpc = "2.0",
            Error = new McpError
            {
                Code = code,
                Message = message,
                Data = data
            },
            Id = id
        };
    }
}