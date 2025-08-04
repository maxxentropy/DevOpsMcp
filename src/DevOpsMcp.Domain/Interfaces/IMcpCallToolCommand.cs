using System.Collections.Generic;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for calling MCP tools from Eagle scripts
/// </summary>
public interface IMcpCallToolCommand
{
    /// <summary>
    /// Calls an MCP tool with the given arguments
    /// </summary>
    string CallTool(string toolName, Dictionary<string, object> arguments);
}