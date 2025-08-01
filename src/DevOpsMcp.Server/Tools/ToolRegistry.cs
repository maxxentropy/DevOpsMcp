using System.Collections.Concurrent;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools;

public sealed class ToolRegistry(ILogger<ToolRegistry> logger) : IToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools = new();

    public Task<List<Tool>> GetToolsAsync()
    {
        var tools = _tools.Values.Select(t => new Tool
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema
        }).ToList();

        return Task.FromResult(tools);
    }

    public async Task<CallToolResponse> CallToolAsync(string toolName, JsonElement? arguments, CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            logger.LogWarning("Tool {ToolName} not found", toolName);
            return new CallToolResponse
            {
                Content = new List<ToolContent>
                {
                    new()
                    {
                        Type = "text",
                        Text = $"Tool '{toolName}' not found"
                    }
                },
                IsError = true
            };
        }

        try
        {
            logger.LogInformation("Executing tool {ToolName}", toolName);
            return await tool.ExecuteAsync(arguments, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return new CallToolResponse
            {
                Content = new List<ToolContent>
                {
                    new()
                    {
                        Type = "text",
                        Text = $"Error executing tool: {ex.Message}"
                    }
                },
                IsError = true
            };
        }
    }

    public void RegisterTool(ITool tool)
    {
        if (_tools.TryAdd(tool.Name, tool))
        {
            logger.LogInformation("Registered tool: {ToolName}", tool.Name);
        }
        else
        {
            logger.LogWarning("Tool {ToolName} is already registered", tool.Name);
        }
    }
}