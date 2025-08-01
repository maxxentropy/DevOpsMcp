using DevOpsMcp.Infrastructure.Authentication;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Infrastructure.Repositories;
using DevOpsMcp.Infrastructure.Services;

namespace DevOpsMcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AzureDevOpsOptions>(configuration.GetSection(AzureDevOpsOptions.SectionName));
        
        // Azure DevOps Client Factory
        services.AddSingleton<IAzureDevOpsClientFactory>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AzureDevOpsOptions>>();
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