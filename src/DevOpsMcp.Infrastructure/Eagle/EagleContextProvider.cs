using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Infrastructure.Eagle.Commands;
using Eagle;
using Eagle._Commands;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using _Result = Eagle._Components.Public.Result;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Provides rich context injection for Eagle scripts with MCP integration
/// </summary>
public class EagleContextProvider : IEagleContextProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EagleContextProvider> _logger;
    private readonly IEagleSessionStore _sessionStore;
    private readonly IMcpCallToolCommand _mcpCallTool;
    private readonly IMcpOutputCommand _mcpOutput;

    public EagleContextProvider(
        IServiceProvider serviceProvider,
        ILogger<EagleContextProvider> logger,
        IEagleSessionStore sessionStore,
        IMcpCallToolCommand mcpCallTool,
        IMcpOutputCommand mcpOutput)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sessionStore = sessionStore;
        _mcpCallTool = mcpCallTool;
        _mcpOutput = mcpOutput;
    }

    /// <summary>
    /// Injects rich context commands into a script interpreter
    /// </summary>
    public void InjectRichContext(IScriptInterpreter interpreter, DevOpsContext? devOpsContext = null, string? sessionId = null)
    {
        try
        {
            // Get the underlying Eagle interpreter
            if (interpreter is not EagleInterpreterAdapter adapter)
            {
                throw new ArgumentException("Interpreter must be an EagleInterpreterAdapter", nameof(interpreter));
            }
            
            var eagleInterpreter = adapter.UnderlyingInterpreter;
            
            // Store session ID in interpreter for commands to access
            if (!string.IsNullOrEmpty(sessionId))
            {
                interpreter.SetVariable("_sessionId", sessionId);
            }

            // Create context command with CommandData
            var contextCommandData = new CommandData(
                "mcp::context",
                null,
                "Provides access to DevOps context information",
                ClientData.Empty,
                typeof(ContextCommand).FullName,
                CommandFlags.None,
                null,
                0);
            var contextCommand = new ContextCommand(
                contextCommandData, 
                devOpsContext,
                _serviceProvider.GetRequiredService<ILogger<ContextCommand>>());

            // Create command instances
            var sessionManager = new EagleSessionManager(_sessionStore, sessionId ?? "default");
            
            // Create session command with CommandData
            var sessionCommandData = new CommandData(
                "mcp::session",
                null,
                "Manages persistent session state for Eagle scripts",
                ClientData.Empty,
                typeof(SessionCommand).FullName,
                CommandFlags.None,
                null,
                0);
            var sessionCommand = new SessionCommand(sessionCommandData, sessionManager);
            
            // Create call_tool command with CommandData
            var callToolCommandData = new CommandData(
                "mcp::call_tool",
                null,
                "Calls MCP tools from Eagle scripts",
                ClientData.Empty,
                typeof(CallToolCommand).FullName,
                CommandFlags.None,
                null,
                0);
            var callToolCommand = new CallToolCommand(callToolCommandData, _mcpCallTool);
            
            // Create mcp::output command
            var outputCommandData = new CommandData(
                "mcp::output",
                null,
                "Formats output in various formats (json, xml, yaml, table, csv, markdown)",
                ClientData.Empty,
                typeof(OutputCommand).FullName,
                CommandFlags.None,
                null,
                0);
            var outputCommand = new OutputCommand(outputCommandData, _mcpOutput);
            
            // Register commands with the interpreter
            try
            {
                // Use the underlying Eagle interpreter for command registration
                _Result? evalResult = null;
                
                // Check if mcp::context command already exists
                _Result? contextCheckResult = null;
                if (eagleInterpreter.EvaluateScript("info commands mcp::context", ref contextCheckResult) == ReturnCode.Ok &&
                    !string.IsNullOrEmpty(contextCheckResult?.ToString()))
                {
                    _logger.LogDebug("mcp::context command already exists, skipping registration");
                }
                else
                {
                    long contextToken = 0;
                    var code = eagleInterpreter.AddCommand(contextCommand, null, ref contextToken, ref evalResult);
                    if (code != ReturnCode.Ok)
                    {
                        _logger.LogError("Failed to add mcp::context command: {Error}", evalResult);
                    }
                    else
                    {
                        _logger.LogDebug("Successfully added mcp::context command");
                    }
                }
                
                // Check if mcp::session command already exists
                _Result? sessionCheckResult = null;
                if (eagleInterpreter.EvaluateScript("info commands mcp::session", ref sessionCheckResult) == ReturnCode.Ok &&
                    !string.IsNullOrEmpty(sessionCheckResult?.ToString()))
                {
                    _logger.LogDebug("mcp::session command already exists, skipping registration");
                }
                else
                {
                    long sessionToken = 0;
                    var code = eagleInterpreter.AddCommand(sessionCommand, null, ref sessionToken, ref evalResult);
                    if (code != ReturnCode.Ok)
                    {
                        _logger.LogError("Failed to add mcp::session command: {Error}", evalResult);
                    }
                    else
                    {
                        _logger.LogDebug("Successfully added mcp::session command");
                    }
                }
                
                // Check if mcp::call_tool command already exists
                _Result? callToolCheckResult = null;
                if (eagleInterpreter.EvaluateScript("info commands mcp::call_tool", ref callToolCheckResult) == ReturnCode.Ok &&
                    !string.IsNullOrEmpty(callToolCheckResult?.ToString()))
                {
                    _logger.LogDebug("mcp::call_tool command already exists, skipping registration");
                }
                else
                {
                    long callToolToken = 0;
                    var code = eagleInterpreter.AddCommand(callToolCommand, null, ref callToolToken, ref evalResult);
                    if (code != ReturnCode.Ok)
                    {
                        _logger.LogError("Failed to add mcp::call_tool command: {Error}", evalResult);
                    }
                    else
                    {
                        _logger.LogDebug("Successfully added mcp::call_tool command");
                    }
                }
                
                // Check if mcp::output command already exists
                _Result? outputCheckResult = null;
                if (eagleInterpreter.EvaluateScript("info commands mcp::output", ref outputCheckResult) == ReturnCode.Ok &&
                    !string.IsNullOrEmpty(outputCheckResult?.ToString()))
                {
                    _logger.LogDebug("mcp::output command already exists, skipping registration");
                }
                else
                {
                    long outputToken = 0;
                    var code = eagleInterpreter.AddCommand(outputCommand, null, ref outputToken, ref evalResult);
                    if (code != ReturnCode.Ok)
                    {
                        _logger.LogError("Failed to add mcp::output command: {Error}", evalResult);
                    }
                    else
                    {
                        _logger.LogDebug("Successfully added mcp::output command");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register commands");
            }

            _logger.LogDebug("Injected rich context commands into Eagle interpreter");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject rich context into Eagle interpreter");
            throw;
        }
    }

    /// <summary>
    /// Creates a context accessor function that retrieves values from DevOpsContext
    /// </summary>
    private string CreateContextAccessor(DevOpsContext? context)
    {
        if (context == null)
            return "                \"*\" { return \"\" }";

        // Build a Tcl switch statement for context access
        var accessor = new System.Text.StringBuilder();
        
        // User context
        if (context.User != null)
        {
            accessor.AppendLine($@"                ""user.id"" {{ return ""{EscapeTclString(context.User.Id)}"" }}");
            accessor.AppendLine($@"                ""user.name"" {{ return ""{EscapeTclString(context.User.Name)}"" }}");
            accessor.AppendLine($@"                ""user.role"" {{ return ""{EscapeTclString(context.User.Role)}"" }}");
            accessor.AppendLine($@"                ""user.experience"" {{ return ""{EscapeTclString(context.User.ExperienceLevel)}"" }}");
            accessor.AppendLine($@"                ""user.timezone"" {{ return ""{EscapeTclString(context.User.TimeZone)}"" }}");
        }

        // Project context
        if (context.Project != null)
        {
            accessor.AppendLine($@"                ""project.id"" {{ return ""{EscapeTclString(context.Project.ProjectId)}"" }}");
            accessor.AppendLine($@"                ""project.name"" {{ return ""{EscapeTclString(context.Project.Name)}"" }}");
            accessor.AppendLine($@"                ""project.stage"" {{ return ""{EscapeTclString(context.Project.Stage)}"" }}");
            accessor.AppendLine($@"                ""project.type"" {{ return ""{EscapeTclString(context.Project.Type)}"" }}");
            accessor.AppendLine($@"                ""project.priority"" {{ return ""{EscapeTclString(context.Project.Priority)}"" }}");
            accessor.AppendLine($@"                ""project.methodology"" {{ return ""{EscapeTclString(context.Project.Methodology)}"" }}");
        }

        // Environment context
        if (context.Environment != null)
        {
            accessor.AppendLine($@"                ""environment.type"" {{ return ""{EscapeTclString(context.Environment.EnvironmentType)}"" }}");
            accessor.AppendLine($@"                ""environment.isProduction"" {{ return ""{context.Environment.IsProduction.ToString().ToLower(CultureInfo.InvariantCulture)}"" }}");
            accessor.AppendLine($@"                ""environment.isRegulated"" {{ return ""{context.Environment.IsRegulated.ToString().ToLower(CultureInfo.InvariantCulture)}"" }}");
            
            if (context.Environment.Regions.Any())
            {
                accessor.AppendLine($@"                ""environment.regions"" {{ return ""{EscapeTclString(string.Join(",", context.Environment.Regions))}"" }}");
            }
        }

        // Tech stack context
        if (context.TechStack != null)
        {
            accessor.AppendLine($@"                ""organization.cloudProvider"" {{ return ""{EscapeTclString(context.TechStack.CloudProvider)}"" }}");
            accessor.AppendLine($@"                ""organization.cicdPlatform"" {{ return ""{EscapeTclString(context.TechStack.CiCdPlatform)}"" }}");
            
            if (context.TechStack.Languages.Any())
            {
                accessor.AppendLine($@"                ""organization.languages"" {{ return ""{EscapeTclString(string.Join(",", context.TechStack.Languages))}"" }}");
            }
            
            if (context.TechStack.Frameworks.Any())
            {
                accessor.AppendLine($@"                ""organization.frameworks"" {{ return ""{EscapeTclString(string.Join(",", context.TechStack.Frameworks))}"" }}");
            }
        }

        return accessor.ToString().TrimEnd();
    }

    private static string EscapeTclString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return "";
            
        // Escape special Tcl characters
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("$", "\\$")
                  .Replace("[", "\\[")
                  .Replace("]", "\\]");
    }
}