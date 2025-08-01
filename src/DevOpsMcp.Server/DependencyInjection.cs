using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Server.Protocols;
using DevOpsMcp.Server.Tools;
using DevOpsMcp.Server.Tools.Projects;
using DevOpsMcp.Server.Tools.WorkItems;

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
        services.AddSingleton<IProtocolHandler>(provider => provider.GetRequiredService<StdioProtocolHandler>());
        
        // Tools
        services.AddTransient<ListProjectsTool>();
        services.AddTransient<CreateProjectTool>();
        services.AddTransient<CreateWorkItemTool>();
        services.AddTransient<QueryWorkItemsTool>();
        
        // Register tools with the registry
        services.AddHostedService<ToolRegistrationService>();
        
        return services;
    }
}

internal sealed class ToolRegistrationService : IHostedService
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ToolRegistrationService> _logger;

    public ToolRegistrationService(
        IToolRegistry toolRegistry,
        IServiceProvider serviceProvider,
        ILogger<ToolRegistrationService> logger)
    {
        _toolRegistry = toolRegistry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering MCP tools");

        // Project tools
        _toolRegistry.RegisterTool(_serviceProvider.GetRequiredService<ListProjectsTool>());
        _toolRegistry.RegisterTool(_serviceProvider.GetRequiredService<CreateProjectTool>());
        
        // Work item tools
        _toolRegistry.RegisterTool(_serviceProvider.GetRequiredService<CreateWorkItemTool>());
        _toolRegistry.RegisterTool(_serviceProvider.GetRequiredService<QueryWorkItemsTool>());
        
        // TODO: Register additional tools as they are implemented
        
        _logger.LogInformation("Tool registration completed");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}