using System.Text.Json.Serialization;
using DevOpsMcp.Server.Tools;

namespace DevOpsMcp.Server.Mcp;

[JsonSerializable(typeof(McpRequest))]
[JsonSerializable(typeof(McpResponse))]
[JsonSerializable(typeof(McpNotification))]
[JsonSerializable(typeof(McpError))]
[JsonSerializable(typeof(InitializeRequest))]
[JsonSerializable(typeof(InitializeResponse))]
[JsonSerializable(typeof(ServerInfo))]
[JsonSerializable(typeof(ServerCapabilities))]
[JsonSerializable(typeof(ToolsCapability))]
[JsonSerializable(typeof(LoggingCapability))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(ListToolsResponse))]
[JsonSerializable(typeof(List<Tool>))]
[JsonSerializable(typeof(Tool))]
[JsonSerializable(typeof(CallToolRequest))]
[JsonSerializable(typeof(CallToolResponse))]
[JsonSerializable(typeof(List<ToolContent>))]
[JsonSerializable(typeof(ToolContent))]
[JsonSerializable(typeof(ProgressNotification))]
[JsonSerializable(typeof(PingEvent))]
[JsonSerializable(typeof(PongResponse))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(object))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
public partial class McpJsonSerializerContext: JsonSerializerContext
{
}