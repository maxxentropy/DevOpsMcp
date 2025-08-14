using System.Diagnostics.CodeAnalysis;

// Server needs public types for MCP protocol interfaces
[assembly: SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "MCP server requires public types for protocol interfaces and tool discovery")]