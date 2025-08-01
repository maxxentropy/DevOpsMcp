using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    
    // Add Serilog
    builder.Host.UseSerilog();
    
    // Add services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMcpServer();
    
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
            .AddConsoleExporter());
    
    // Add health checks
    builder.Services.AddHealthChecks();
    
    // Add API endpoints
    builder.Services.AddEndpointsApiExplorer();
    
    var app = builder.Build();
    
    // Configure pipeline
    
    app.UseSerilogRequestLogging();
    app.UseHealthChecks("/health");
    
    // Map endpoints
    
    // SSE endpoint
    app.MapGet("/sse", async (HttpContext context, SseProtocolHandler handler) =>
    {
        await handler.HandleConnectionAsync(context);
    });
    
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