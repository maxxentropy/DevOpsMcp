using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using DevOpsMcp.Server;
using DevOpsMcp.Server.Protocols;
using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Application;
using DevOpsMcp.Infrastructure;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Infrastructure.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/devops-mcp-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

await RunAsync(args);

static async Task RunAsync(string[] args)
{
try
{
    Log.Information("Starting DevOps MCP Server");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Explicitly add environment variables to configuration
    builder.Configuration.AddEnvironmentVariables();
    
    // Add Serilog
    builder.Host.UseSerilog();
    
    // Add services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMcpServer();
    
    // Add distributed cache (required by PersonaMemoryManager)
    builder.Services.AddDistributedMemoryCache();
    
    // Add persona services
    builder.Services.AddPersonaServices();
    
    // Add OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService("DevOpsMcp.Server"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddPrometheusExporter());
    
    // Add health checks
    builder.Services.AddHealthChecks();
    
    // Add API endpoints
    builder.Services.AddEndpointsApiExplorer();
    
    // Add CORS support for Streamable HTTP
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Mcp-Session-Id");
        });
    });
    
    var app = builder.Build();
    
    // Test Azure DevOps connection on startup
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var clientFactory = scope.ServiceProvider.GetRequiredService<IAzureDevOpsClientFactory>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<AzureDevOpsOptions>>();
            
            Log.Information("Testing Azure DevOps connection to {Organization}", 
                new Uri(options.Value.OrganizationUrl).Host);
            
            // Try to create a client to validate the connection
            var projectClient = clientFactory.CreateProjectClient();
            
            Log.Information("Successfully initialized Azure DevOps client factory");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize Azure DevOps connection. Check your AZURE_DEVOPS_ORG_URL and AZURE_DEVOPS_PAT environment variables.");
            // Don't throw - allow server to start for debugging
            Log.Warning("Server starting in degraded mode - Azure DevOps connection failed");
        }
    }
    
    // Configure pipeline
    
    app.UseSerilogRequestLogging();
    app.UseCors(); // Enable CORS
    app.UseHealthChecks("/health");
    app.UseOpenTelemetryPrometheusScrapingEndpoint(); // Add Prometheus metrics endpoint at /metrics
    
    // Map endpoints
    
    // Authentication diagnostics endpoint
    app.MapGet("/debug/auth", (IConfiguration configuration, IOptions<AzureDevOpsOptions> options, ILogger<Program> logger) =>
    {
        var diagnostics = new
        {
            Configuration = new
            {
                OrganizationUrl = options.Value.OrganizationUrl ?? "NOT SET",
                HasPersonalAccessToken = !string.IsNullOrEmpty(options.Value.PersonalAccessToken),
                AuthMethod = options.Value.AuthMethod.ToString()
            },
            EnvironmentVariables = new
            {
                AZURE_DEVOPS_ORG_URL = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG_URL") ?? "NOT SET",
                HasAZURE_DEVOPS_PAT = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT")),
                AzureDevOps__OrganizationUrl = Environment.GetEnvironmentVariable("AzureDevOps__OrganizationUrl") ?? "NOT SET",
                HasAzureDevOps__PersonalAccessToken = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AzureDevOps__PersonalAccessToken"))
            },
            ConfigurationSources = configuration.AsEnumerable()
                .Where(kvp => kvp.Key.StartsWith("AzureDevOps", StringComparison.OrdinalIgnoreCase))
                .Select(kvp => new { Key = kvp.Key, HasValue = !string.IsNullOrEmpty(kvp.Value) })
                .ToList()
        };
        
        logger.LogInformation("Authentication diagnostics requested: {@Diagnostics}", diagnostics);
        return Results.Ok(diagnostics);
    });
    
    // Streamable HTTP endpoint (unified endpoint for both GET and POST)
    app.MapMcp("/mcp");
    
    // Legacy SSE endpoint (for backward compatibility)
    app.MapGet("/sse", async (HttpContext context, SseProtocolHandler handler) =>
    {
        await handler.HandleConnectionAsync(context);
    });
    
    // Legacy RPC endpoint (for backward compatibility)
    app.MapPost("/rpc", async (HttpContext context, SseProtocolHandler handler) =>
    {
        var request = await context.Request.ReadFromJsonAsync<McpRequest>();
        if (request == null)
        {
            return Results.BadRequest("Invalid request");
        }
        return await handler.HandleRequestAsync(context, request);
    });
    
    // Start protocol handlers based on configuration
    var protocolMode = app.Configuration["MCP:Protocol"] ?? "stdio";
    
    if (protocolMode == "stdio")
    {
        var stdioHandler = app.Services.GetRequiredService<StdioProtocolHandler>();
        await stdioHandler.StartAsync();
        
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            stdioHandler.StopAsync().GetAwaiter().GetResult();
        });
    }
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
}