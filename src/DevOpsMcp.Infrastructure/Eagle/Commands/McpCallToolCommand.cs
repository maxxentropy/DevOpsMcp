using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Infrastructure.Eagle.Commands;

/// <summary>
/// Helper class to call MCP tools from Eagle scripts
/// </summary>
public class McpCallToolCommand : IMcpCallToolCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<McpCallToolCommand> _logger;
    
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };
    
    public McpCallToolCommand(IServiceProvider serviceProvider, ILogger<McpCallToolCommand> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Calls an MCP tool and returns the result as a string
    /// </summary>
    public string CallTool(string toolName, Dictionary<string, object> arguments)
    {
        try
        {
            _logger.LogInformation("MCP tool call requested: {ToolName} with arguments: {Arguments}",
                toolName, arguments != null ? JsonSerializer.Serialize(arguments) : "none");
            
            // Call the tool asynchronously and wait for result
            var result = CallToolAsync(toolName, arguments).GetAwaiter().GetResult();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MCP tool {ToolName}", toolName);
            return $"ERROR: {ex.Message}";
        }
    }
    
    private async Task<string> CallToolAsync(string toolName, Dictionary<string, object>? arguments)
    {
        // Get the tool registry from DI
        // Note: We can't inject IToolRegistry directly because it's in the Server assembly
        // which Infrastructure can't reference. So we use dynamic resolution.
        var toolRegistryType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "IToolRegistry" && t.IsInterface);
            
        if (toolRegistryType == null)
        {
            throw new System.InvalidOperationException("IToolRegistry interface not found");
        }
        
        var toolRegistry = _serviceProvider.GetService(toolRegistryType);
        if (toolRegistry == null)
        {
            throw new System.InvalidOperationException("Tool registry not available");
        }
        
        // Convert arguments to JsonElement
        JsonElement? jsonArgs = null;
        if (arguments != null && arguments.Count > 0)
        {
            var json = JsonSerializer.Serialize(arguments);
            jsonArgs = JsonDocument.Parse(json).RootElement;
        }
        
        // Use reflection to call the method
        var callToolMethod = toolRegistryType.GetMethod("CallToolAsync");
        if (callToolMethod == null)
        {
            throw new System.InvalidOperationException("CallToolAsync method not found");
        }
        
        // Invoke the method
        var task = callToolMethod.Invoke(toolRegistry, new object?[] { toolName, jsonArgs, CancellationToken.None }) as Task;
        if (task == null)
        {
            throw new System.InvalidOperationException("Failed to invoke CallToolAsync");
        }
        
        await task;
        
        // Get the result
        var resultProperty = task.GetType().GetProperty("Result");
        var callToolResponse = resultProperty?.GetValue(task);
        
        if (callToolResponse == null)
        {
            throw new System.InvalidOperationException("No response from tool");
        }
        
        // Extract the response content
        var contentProperty = callToolResponse.GetType().GetProperty("Content");
        var content = contentProperty?.GetValue(callToolResponse);
        
        if (content == null)
        {
            return "Tool returned no content";
        }
        
        // The content is a list of ToolContent objects
        // Convert to a readable format
        var contentList = content as System.Collections.IList;
        if (contentList != null && contentList.Count > 0)
        {
            var firstContent = contentList[0];
            var textProperty = firstContent?.GetType().GetProperty("Text");
            var text = textProperty?.GetValue(firstContent) as string;
            
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }
        
        // Fallback: serialize the entire response
        return JsonSerializer.Serialize(callToolResponse, IndentedJsonOptions);
    }
}