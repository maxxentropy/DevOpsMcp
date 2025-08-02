using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Server.Protocols;
using DevOpsMcp.Server.Tools;
using DevOpsMcp.Server.Tools.Projects;
using DevOpsMcp.Server.Tools.WorkItems;
using DevOpsMcp.Server.Tools.Personas;

namespace DevOpsMcp.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddMcpServer(this IServiceCollection services)
    {
        // MCP Core
        services.AddSingleton<IMessageHandler, MessageHandler>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        
        // Protocol Handlers
        services.AddSingleton<StdioProtocolHandler>();
        services.AddSingleton<SseProtocolHandler>();
        services.AddSingleton<StreamableHttpHandler>();
        services.AddSingleton<IProtocolHandler>(provider => provider.GetRequiredService<StdioProtocolHandler>());
        
        // Tools
        services.AddTransient<ListProjectsTool>();
        services.AddTransient<CreateProjectTool>();
        services.AddTransient<CreateWorkItemTool>();
        services.AddTransient<QueryWorkItemsTool>();
        
        // Persona Tools
        services.AddTransient<ActivatePersonaTool>();
        services.AddTransient<SelectPersonaTool>();
        services.AddTransient<InteractWithPersonaTool>();
        services.AddTransient<GetPersonaStatusTool>();
        services.AddTransient<ManagePersonaMemoryTool>();
        services.AddTransient<ConfigurePersonaBehaviorTool>();
        
        // Register tools with the registry
        services.AddHostedService<ToolRegistrationService>();
        
        return services;
    }
}

internal sealed class ToolRegistrationService(
    IToolRegistry toolRegistry,
    IServiceProvider serviceProvider,
    ILogger<ToolRegistrationService> logger)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering MCP tools");

        // Project tools
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ListProjectsTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<CreateProjectTool>());
        
        // Work item tools
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<CreateWorkItemTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<QueryWorkItemsTool>());
        
        // Persona tools
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ActivatePersonaTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<SelectPersonaTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<InteractWithPersonaTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<GetPersonaStatusTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ManagePersonaMemoryTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ConfigurePersonaBehaviorTool>());
        
        logger.LogInformation("Tool registration completed");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}