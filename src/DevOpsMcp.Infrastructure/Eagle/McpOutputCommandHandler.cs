using System;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Handler for MCP output command
/// </summary>
public class McpOutputCommandHandler : IMcpOutputCommand
{
    private readonly IEagleOutputFormatter _outputFormatter;
    private readonly ILogger<McpOutputCommandHandler> _logger;
    
    public McpOutputCommandHandler(
        IEagleOutputFormatter outputFormatter,
        ILogger<McpOutputCommandHandler> logger)
    {
        _outputFormatter = outputFormatter;
        _logger = logger;
    }
    
    public string FormatOutput(object data, string format)
    {
        try
        {
            _logger.LogDebug("Formatting output in {Format} format", format);
            
            // Convert object data to string
            string dataStr;
            if (data is string s)
            {
                dataStr = s;
            }
            else
            {
                // Convert to JSON string if not already a string
                dataStr = System.Text.Json.JsonSerializer.Serialize(data);
            }
            
            // Parse format to OutputFormat enum
            if (!Enum.TryParse<OutputFormat>(format, true, out var outputFormat))
            {
                throw new ArgumentException($"Invalid output format: {format}");
            }
            
            var result = _outputFormatter.Format(dataStr, outputFormat);
            return result.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting output in {Format} format", format);
            throw;
        }
    }
}