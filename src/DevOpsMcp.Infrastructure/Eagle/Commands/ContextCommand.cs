using System;
using Eagle._Attributes;
using Eagle._Commands;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Infrastructure.Eagle.Commands;

/// <summary>
/// Eagle command that provides access to DevOps context information
/// Usage: mcp::context get key.path
/// </summary>
[ObjectId("8a5f2d3e-7b1c-4e9f-a2d8-3c5e7f9a1b4d")]
[CommandFlags(CommandFlags.None)]
[ObjectGroup("mcp")]
public sealed class ContextCommand : Default
{
    private readonly ILogger<ContextCommand> _logger;
    private readonly DevOpsContext? _devOpsContext;

    public ContextCommand(
        ICommandData commandData,
        DevOpsContext? devOpsContext,
        ILogger<ContextCommand> logger)
        : base(commandData)
    {
        _devOpsContext = devOpsContext;
        _logger = logger;
    }

    public override ReturnCode Execute(
        Interpreter interpreter,
        IClientData clientData,
        ArgumentList arguments,
        ref Result result)
    {
        try
        {
            // Validate arguments: mcp::context get key.path
            if (arguments.Count != 3)
            {
                result = "wrong # args: should be \"mcp::context get key.path\"";
                return ReturnCode.Error;
            }

            var action = arguments[1].ToString();
            var keyPath = arguments[2].ToString();

            if (action != "get")
            {
                result = $"unknown action \"{action}\": must be get";
                return ReturnCode.Error;
            }

            // Get context value based on key path
            var value = GetContextValue(keyPath);
            result = value;
            return ReturnCode.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing mcp::context command");
            result = $"error accessing context: {ex.Message}";
            return ReturnCode.Error;
        }
    }

    private string GetContextValue(string keyPath)
    {
        if (_devOpsContext == null)
        {
            return string.Empty;
        }

        // Parse the key path (e.g., "user.name", "project.id", "environment.type", "project.lastBuild.status")
        var parts = keyPath.Split('.');
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        var category = parts[0].ToLowerInvariant();
        
        // Build the remaining path for deep access
        var remainingPath = parts.Length > 1 
            ? string.Join(".", parts.Skip(1)) 
            : string.Empty;

        return category switch
        {
            "user" => GetUserValue(remainingPath),
            "project" => GetProjectValue(remainingPath),
            "organization" => GetOrganizationValue(remainingPath),
            "environment" => GetEnvironmentValue(remainingPath),
            "techstack" => GetTechStackValue(remainingPath),
            "team" => GetTeamValue(remainingPath),
            _ => string.Empty
        };
    }

    private string GetUserValue(string property)
    {
        if (_devOpsContext?.User == null)
        {
            return string.Empty;
        }

        return property switch
        {
            "id" => _devOpsContext.User.Id ?? string.Empty,
            "name" => _devOpsContext.User.Name ?? string.Empty,
            "role" => _devOpsContext.User.Role ?? string.Empty,
            "experiencelevel" => _devOpsContext.User.ExperienceLevel ?? string.Empty,
            "timezone" => _devOpsContext.User.TimeZone ?? string.Empty,
            _ => string.Empty
        };
    }

    private string GetProjectValue(string property)
    {
        if (_devOpsContext?.Project == null)
        {
            return string.Empty;
        }

        // Handle deep paths like "lastBuild.status"
        var parts = property.Split('.');
        var firstPart = parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;
        
        switch (firstPart)
        {
            case "id":
                return _devOpsContext.Project.ProjectId ?? string.Empty;
            case "name":
                return _devOpsContext.Project.Name ?? string.Empty;
            case "stage":
                return _devOpsContext.Project.Stage ?? string.Empty;
            case "type":
                return _devOpsContext.Project.Type ?? string.Empty;
            case "priority":
                return _devOpsContext.Project.Priority ?? string.Empty;
            case "methodology":
                return _devOpsContext.Project.Methodology ?? string.Empty;
            case "lastbuild":
                // Handle lastBuild.* paths
                if (parts.Length > 1)
                {
                    var buildProperty = parts[1].ToLowerInvariant();
                    return buildProperty switch
                    {
                        "status" => "Succeeded", // Simulated for now
                        "id" => "Build-12345",   // Simulated for now
                        "date" => DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"), // Simulated
                        _ => string.Empty
                    };
                }
                return string.Empty;
            case "repository":
                // Handle repository.* paths
                if (parts.Length > 1)
                {
                    var repoProperty = parts[1].ToLowerInvariant();
                    return repoProperty switch
                    {
                        "url" => "https://dev.azure.com/org/project/_git/repo", // Simulated
                        "branch" => "main", // Simulated
                        _ => string.Empty
                    };
                }
                return string.Empty;
            default:
                return string.Empty;
        }
    }

    private string GetOrganizationValue(string property)
    {
        // DevOpsContext doesn't have Organization directly, check TechStack
        if (_devOpsContext?.TechStack == null)
        {
            return string.Empty;
        }

        return property switch
        {
            "cloudprovider" => _devOpsContext.TechStack.CloudProvider ?? string.Empty,
            "cicdplatform" => _devOpsContext.TechStack.CiCdPlatform ?? string.Empty,
            "name" => "DevOps Organization", // Default value since not in context
            _ => string.Empty
        };
    }

    private string GetEnvironmentValue(string property)
    {
        if (_devOpsContext?.Environment == null)
        {
            _logger.LogWarning("Environment context is null");
            return string.Empty;
        }

        var lowerProperty = property.ToLowerInvariant();
        return lowerProperty switch
        {
            "type" => _devOpsContext.Environment.EnvironmentType ?? string.Empty,
            "isproduction" => _devOpsContext.Environment.IsProduction.ToString().ToLowerInvariant(),
            "isdevelopment" => (!_devOpsContext.Environment.IsProduction).ToString().ToLowerInvariant(),
            "isstaging" => (_devOpsContext.Environment.EnvironmentType?.Equals("Staging", StringComparison.OrdinalIgnoreCase) ?? false).ToString().ToLowerInvariant(),
            "isregulated" => _devOpsContext.Environment.IsRegulated.ToString().ToLowerInvariant(),
            _ => string.Empty
        };
    }

    private string GetTechStackValue(string property)
    {
        if (_devOpsContext?.TechStack == null)
        {
            return string.Empty;
        }

        return property switch
        {
            "cloudprovider" => _devOpsContext.TechStack.CloudProvider ?? string.Empty,
            "cicdplatform" => _devOpsContext.TechStack.CiCdPlatform ?? string.Empty,
            "languages" => string.Join(",", _devOpsContext.TechStack.Languages),
            "frameworks" => string.Join(",", _devOpsContext.TechStack.Frameworks),
            _ => string.Empty
        };
    }

    private string GetTeamValue(string property)
    {
        if (_devOpsContext?.Team == null)
        {
            return string.Empty;
        }

        return property switch
        {
            "size" => _devOpsContext.Team.TeamSize.ToString(),
            "maturity" => _devOpsContext.Team.TeamMaturity ?? string.Empty,
            "collaborationstyle" => _devOpsContext.Team.CollaborationStyle ?? string.Empty,
            _ => string.Empty
        };
    }
}