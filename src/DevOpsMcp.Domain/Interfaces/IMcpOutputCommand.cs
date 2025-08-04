namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for MCP output command handler
/// </summary>
public interface IMcpOutputCommand
{
    /// <summary>
    /// Format and output data in the specified format
    /// </summary>
    /// <param name="data">The data to format (can be JSON string or object)</param>
    /// <param name="format">The output format (json, xml, yaml, table, csv, markdown)</param>
    /// <returns>Formatted output string</returns>
    string FormatOutput(object data, string format);
}