using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Server.Protocols;
using DevOpsMcp.Server.Tools;
using DevOpsMcp.Server.Tools.Projects;
using DevOpsMcp.Server.Tools.WorkItems;
using DevOpsMcp.Server.Tools.Personas;
using DevOpsMcp.Server.Tools.Eagle;
using DevOpsMcp.Server.Tools.Email;
using DevOpsMcp.Server.Tools.Enhanced;
using DevOpsMcp.Infrastructure.Configuration;

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
        services.AddTransient<GetRecentWorkItemsTool>();
        services.AddTransient<GetWorkItemByIdTool>();
        
        // Persona Tools
        services.AddTransient<ActivatePersonaTool>();
        services.AddTransient<SelectPersonaTool>();
        services.AddTransient<InteractWithPersonaTool>();
        services.AddTransient<GetPersonaStatusTool>();
        services.AddTransient<ManagePersonaMemoryTool>();
        services.AddTransient<ConfigurePersonaBehaviorTool>();
        
        // Eagle Tools
        services.AddTransient<EagleExecutionTool>();
        services.AddTransient<EagleHistoryTool>();
        
        // Email Tools
        services.AddTransient<SendEmailTool>();
        services.AddTransient<SendTemplatedEmailTool>();
        services.AddTransient<SendTeamEmailTool>();
        services.AddTransient<GetSendQuotaTool>();
        services.AddTransient<GetSendStatisticsTool>();
        
        // Enhanced Feature Tools
        services.AddTransient<CreateTaskTool>();
        services.AddTransient<ListTasksTool>();
        services.AddTransient<UpdateTaskTool>();
        services.AddTransient<ManageProjectTool>();
        services.AddTransient<SearchKnowledgeTool>();
        
        // Register tools with the registry
        services.AddHostedService<ToolRegistrationService>();
        
        return services;
    }
}

internal sealed class ToolRegistrationService(
    IToolRegistry toolRegistry,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
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
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<GetRecentWorkItemsTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<GetWorkItemByIdTool>());
        
        // Persona tools
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ActivatePersonaTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<SelectPersonaTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<InteractWithPersonaTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<GetPersonaStatusTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ManagePersonaMemoryTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ConfigurePersonaBehaviorTool>());
        
        // Eagle tools
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<EagleExecutionTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<EagleHistoryTool>());
        
        // Email tools
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<SendEmailTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<SendTemplatedEmailTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<SendTeamEmailTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<GetSendQuotaTool>());
        toolRegistry.RegisterTool(serviceProvider.GetRequiredService<GetSendStatisticsTool>());
        
        // Enhanced Feature tools - Only register if configured
        var enhancedOptions = configuration
            .GetSection(EnhancedFeaturesOptions.SectionName)
            .Get<EnhancedFeaturesOptions>();
            
        if (!string.IsNullOrEmpty(enhancedOptions?.DatabaseUrl))
        {
            logger.LogInformation("Registering Enhanced Feature tools");
            toolRegistry.RegisterTool(serviceProvider.GetRequiredService<CreateTaskTool>());
            toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ListTasksTool>());
            toolRegistry.RegisterTool(serviceProvider.GetRequiredService<UpdateTaskTool>());
            toolRegistry.RegisterTool(serviceProvider.GetRequiredService<ManageProjectTool>());
            toolRegistry.RegisterTool(serviceProvider.GetRequiredService<SearchKnowledgeTool>());
        }
        
        logger.LogInformation("Tool registration completed");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}