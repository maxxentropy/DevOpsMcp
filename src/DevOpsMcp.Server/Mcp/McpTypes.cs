using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevOpsMcp.Server.Mcp;

public sealed record McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public required string Jsonrpc { get; init; } = "2.0";
    
    [JsonPropertyName("method")]
    public required string Method { get; init; }
    
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
    
    [JsonPropertyName("id")]
    public object? Id { get; init; }
}

public sealed record McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public required string Jsonrpc { get; init; } = "2.0";
    
    [JsonPropertyName("result")]
    public object? Result { get; init; }
    
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpError? Error { get; init; }
    
    [JsonPropertyName("id")]
    public required object Id { get; init; }
}

public sealed record McpError
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }
    
    [JsonPropertyName("message")]
    public required string Message { get; init; }
    
    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

public sealed record McpNotification
{
    [JsonPropertyName("jsonrpc")]
    public required string Jsonrpc { get; init; } = "2.0";
    
    [JsonPropertyName("method")]
    public required string Method { get; init; }
    
    [JsonPropertyName("params")]
    public object? Params { get; init; }
}

public sealed record InitializeRequest
{
    [JsonPropertyName("protocolVersion")]
    public required string ProtocolVersion { get; init; }
    
    [JsonPropertyName("capabilities")]
    public required ClientCapabilities Capabilities { get; init; }
    
    [JsonPropertyName("clientInfo")]
    public ClientInfo? ClientInfo { get; init; }
}

public sealed record ClientCapabilities
{
    [JsonPropertyName("experimental")]
    public ExperimentalCapabilities? Experimental { get; init; }
    
    [JsonPropertyName("sampling")]
    public SamplingCapabilities? Sampling { get; init; }
}

public sealed record ExperimentalCapabilities
{
    [JsonPropertyName("features")]
    public Dictionary<string, object>? Features { get; init; }
}

public sealed record SamplingCapabilities
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }
}

public sealed record ClientInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

public sealed record InitializeResponse
{
    [JsonPropertyName("protocolVersion")]
    public required string ProtocolVersion { get; init; }
    
    [JsonPropertyName("capabilities")]
    public required ServerCapabilities Capabilities { get; init; }
    
    [JsonPropertyName("serverInfo")]
    public required ServerInfo ServerInfo { get; init; }
}

public sealed record ServerCapabilities
{
    [JsonPropertyName("tools")]
    public ToolsCapability? Tools { get; init; }
    
    [JsonPropertyName("prompts")]
    public PromptsCapability? Prompts { get; init; }
    
    [JsonPropertyName("resources")]
    public ResourcesCapability? Resources { get; init; }
    
    [JsonPropertyName("logging")]
    public LoggingCapability? Logging { get; init; }
}

public sealed record ToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

public sealed record PromptsCapability
{
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

public sealed record ResourcesCapability
{
    [JsonPropertyName("subscribe")]
    public bool? Subscribe { get; init; }
    
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

public sealed record LoggingCapability
{
    [JsonPropertyName("levels")]
    public List<string>? Levels { get; init; }
}

public sealed record ServerInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

public sealed record Tool
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("description")]
    public required string Description { get; init; }
    
    [JsonPropertyName("inputSchema")]
    public required JsonElement InputSchema { get; init; }
}

public sealed record CallToolRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; init; }
}

public sealed record CallToolResponse
{
    [JsonPropertyName("content")]
    public List<ToolContent> Content { get; init; } = new();
    
    [JsonPropertyName("isError")]
    public bool? IsError { get; init; }
}

public sealed record ToolContent
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }
    
    [JsonPropertyName("text")]
    public string? Text { get; init; }
    
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }
    
    [JsonPropertyName("data")]
    public string? Data { get; init; }
}

public sealed record ListToolsResponse
{
    [JsonPropertyName("tools")]
    public List<Tool> Tools { get; init; } = new();
}

public sealed record ProgressNotification
{
    [JsonPropertyName("progressToken")]
    public required string ProgressToken { get; init; }
    
    [JsonPropertyName("progress")]
    public required double Progress { get; init; }
    
    [JsonPropertyName("total")]
    public double? Total { get; init; }
}

public sealed record LogMessageNotification
{
    [JsonPropertyName("level")]
    public required string Level { get; init; }
    
    [JsonPropertyName("logger")]
    public required string Logger { get; init; }
    
    [JsonPropertyName("data")]
    public required object Data { get; init; }
}

public sealed record PingEvent
{
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record PongResponse
{
    [JsonPropertyName("pong")]
    public bool Pong { get; init; } = true;
    
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}