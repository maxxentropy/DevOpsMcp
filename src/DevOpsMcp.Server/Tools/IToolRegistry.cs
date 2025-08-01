using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools;

public interface IToolRegistry
{
    Task<List<Tool>> GetToolsAsync();
    Task<CallToolResponse> CallToolAsync(string toolName, JsonElement? arguments, CancellationToken cancellationToken = default);
    void RegisterTool(ITool tool);
}

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    Task<CallToolResponse> ExecuteAsync(JsonElement? arguments, CancellationToken cancellationToken = default);
}