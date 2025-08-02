using System;
using System.IO;
using DevOpsMcp.Application.Personas.Memory;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Authentication;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Infrastructure.Eagle;
using DevOpsMcp.Infrastructure.Personas.Memory;
using DevOpsMcp.Infrastructure.Repositories;
using DevOpsMcp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

namespace DevOpsMcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AzureDevOpsOptions>(configuration.GetSection(AzureDevOpsOptions.SectionName));
        services.Configure<EagleOptions>(configuration.GetSection(EagleOptions.SectionName));
        
        // Azure DevOps Client Factory - Use factory delegate to ensure proper configuration binding
        services.AddSingleton<IAzureDevOpsClientFactory>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AzureDevOpsOptions>>();
            var logger = provider.GetRequiredService<ILogger<AzureDevOpsClientFactory>>();
            
            // Validate configuration
            if (string.IsNullOrEmpty(options.Value.OrganizationUrl))
            {
                logger.LogError("Azure DevOps OrganizationUrl is not configured. Please set AZURE_DEVOPS_ORG_URL environment variable.");
                throw new System.InvalidOperationException("Azure DevOps OrganizationUrl is required");
            }
            
            if (string.IsNullOrEmpty(options.Value.PersonalAccessToken))
            {
                logger.LogError("Azure DevOps PersonalAccessToken is not configured. Please set AZURE_DEVOPS_PAT environment variable.");
                throw new System.InvalidOperationException("Azure DevOps PersonalAccessToken is required");
            }
            
            logger.LogInformation("Initializing Azure DevOps client factory with organization: {Organization}", 
                new Uri(options.Value.OrganizationUrl).Host);
            
            return new AzureDevOpsClientFactory(options.Value.OrganizationUrl, options.Value.PersonalAccessToken);
        });
        
        // Authentication
        services.AddSingleton<IDevOpsAuthenticationProvider, DevOpsAuthenticationProvider>();
        
        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IBuildRepository, BuildRepository>();
        services.AddScoped<IRepositoryService, RepositoryService>();
        services.AddScoped<IPullRequestService, PullRequestService>();
        
        // Persona Infrastructure
        services.AddSingleton<IPersonaMemoryStore, FileBasedPersonaMemoryStore>();
        services.Configure<PersonaMemoryStoreOptions>(configuration.GetSection("PersonaMemoryStore"));
        
        // Eagle Script Executor
        services.AddSingleton<IEagleScriptExecutor, EagleScriptExecutor>();
        
        return services;
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.TryGetValue("logger", out var loggerValue) ? loggerValue as ILogger : null;
                    logger?.LogWarning("Delaying for {delay}ms, then making retry {retry}.", timespan.TotalMilliseconds, retryCount);
                });
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (result, timespan, context) =>
                {
                    var logger = context.TryGetValue("logger", out var loggerValue) ? loggerValue as ILogger : null;
                    logger?.LogWarning("Circuit breaker opened for {duration}s", timespan.TotalSeconds);
                },
                onReset: context =>
                {
                    var logger = context.TryGetValue("logger", out var loggerValue) ? loggerValue as ILogger : null;
                    logger?.LogInformation("Circuit breaker reset");
                });
    }
}