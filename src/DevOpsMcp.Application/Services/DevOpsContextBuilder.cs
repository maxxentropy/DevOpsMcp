using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Configuration;

namespace DevOpsMcp.Application.Services;

/// <summary>
/// Builds DevOpsContext from various sources
/// </summary>
public interface IDevOpsContextBuilder
{
    DevOpsContext BuildContext(string? projectId = null, string? sessionId = null);
}

public class DevOpsContextBuilder : IDevOpsContextBuilder
{
    private readonly IConfiguration _configuration;

    public DevOpsContextBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DevOpsContext BuildContext(string? projectId = null, string? sessionId = null)
    {
        // Build a basic context from configuration and defaults
        var context = new DevOpsContext
        {
            User = new UserProfile
            {
                Name = _configuration["User:Name"] ?? "DevOps User",
                Role = _configuration["User:Role"] ?? "Developer",
                ExperienceLevel = _configuration["User:ExperienceLevel"] ?? "Intermediate",
                TimeZone = _configuration["User:TimeZone"] ?? "UTC"
            },
            Project = new ProjectMetadata
            {
                ProjectId = projectId ?? _configuration["Project:Id"] ?? "default",
                Name = _configuration["Project:Name"] ?? "DevOps MCP Project",
                Stage = _configuration["Project:Stage"] ?? "Development",
                Type = _configuration["Project:Type"] ?? "Standard",
                Priority = _configuration["Project:Priority"] ?? "Medium",
                Methodology = _configuration["Project:Methodology"] ?? "Agile"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = _configuration["Environment:Type"] ?? "Development",
                IsProduction = bool.Parse(_configuration["Environment:IsProduction"] ?? "false"),
                IsRegulated = bool.Parse(_configuration["Environment:IsRegulated"] ?? "false")
            },
            TechStack = new TechnologyConfiguration
            {
                CloudProvider = _configuration["TechStack:CloudProvider"] ?? "Azure",
                CiCdPlatform = _configuration["TechStack:CiCdPlatform"] ?? "Azure DevOps"
            }
        };

        // Add session context if provided
        if (!string.IsNullOrEmpty(sessionId))
        {
            context.Session = new SessionContext
            {
                SessionId = sessionId,
                StartTime = DateTime.UtcNow
            };
        }

        // Add common tech stack
        context.TechStack.Languages.Add("C#");
        context.TechStack.Languages.Add("Eagle");
        context.TechStack.Frameworks.Add(".NET 8");
        context.TechStack.Frameworks.Add("MCP");
        
        return context;
    }
}