namespace DevOpsMcp.Server.Mcp;

public sealed record McpRequest
{
    public required string Jsonrpc { get; init; } = "2.0";
    public required string Method { get; init; }
    public JsonElement? Params { get; init; }
    public object? Id { get; init; }
}

public sealed record McpResponse
{
    public required string Jsonrpc { get; init; } = "2.0";
    public object? Result { get; init; }
    public McpError? Error { get; init; }
    public required object Id { get; init; }
}

public sealed record McpError
{
    public required int Code { get; init; }
    public required string Message { get; init; }
    public object? Data { get; init; }
}

public sealed record McpNotification
{
    public required string Jsonrpc { get; init; } = "2.0";
    public required string Method { get; init; }
    public object? Params { get; init; }
}

public sealed record InitializeRequest
{
    public required string ProtocolVersion { get; init; }
    public required ClientCapabilities Capabilities { get; init; }
    public ClientInfo? ClientInfo { get; init; }
}

public sealed record ClientCapabilities
{
    public ExperimentalCapabilities? Experimental { get; init; }
    public SamplingCapabilities? Sampling { get; init; }
}

public sealed record ExperimentalCapabilities
{
    public Dictionary<string, object>? Features { get; init; }
}

public sealed record SamplingCapabilities
{
    public bool? Enabled { get; init; }
}

public sealed record ClientInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public sealed record InitializeResponse
{
    public required string ProtocolVersion { get; init; }
    public required ServerCapabilities Capabilities { get; init; }
    public required ServerInfo ServerInfo { get; init; }
}

public sealed record ServerCapabilities
{
    public ToolsCapability? Tools { get; init; }
    public PromptsCapability? Prompts { get; init; }
    public ResourcesCapability? Resources { get; init; }
    public LoggingCapability? Logging { get; init; }
}

public sealed record ToolsCapability
{
    public bool? ListChanged { get; init; }
}

public sealed record PromptsCapability
{
    public bool? ListChanged { get; init; }
}

public sealed record ResourcesCapability
{
    public bool? Subscribe { get; init; }
    public bool? ListChanged { get; init; }
}

public sealed record LoggingCapability
{
    public List<string>? Levels { get; init; }
}

public sealed record ServerInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public sealed record Tool
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required JsonElement InputSchema { get; init; }
}

public sealed record CallToolRequest
{
    public required string Name { get; init; }
    public JsonElement? Arguments { get; init; }
}

public sealed record CallToolResponse
{
    public List<ToolContent> Content { get; init; } = new();
    public bool? IsError { get; init; }
}

public sealed record ToolContent
{
    public required string Type { get; init; }
    public string? Text { get; init; }
    public string? MimeType { get; init; }
    public string? Data { get; init; }
}

public sealed record ListToolsResponse
{
    public List<Tool> Tools { get; init; } = new();
}

public sealed record ProgressNotification
{
    public required string ProgressToken { get; init; }
    public required double Progress { get; init; }
    public double? Total { get; init; }
}

public sealed record LogMessageNotification
{
    public required string Level { get; init; }
    public required string Logger { get; init; }
    public required object Data { get; init; }
}

public sealed record PingEvent
{
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record PongResponse
{
    public bool Pong { get; init; } = true;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}