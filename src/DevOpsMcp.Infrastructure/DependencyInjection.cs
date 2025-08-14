using System;
using Amazon.SimpleEmailV2;
using Amazon.Extensions.NETCore.Setup;
using DevOpsMcp.Application.Personas.Memory;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Authentication;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Infrastructure.Data;
using DevOpsMcp.Infrastructure.Eagle;
using DevOpsMcp.Infrastructure.Eagle.Commands;
using DevOpsMcp.Infrastructure.Email;
using DevOpsMcp.Infrastructure.Personas.Memory;
using DevOpsMcp.Infrastructure.Repositories;
using DevOpsMcp.Infrastructure.Repositories.Enhanced;
using DevOpsMcp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Supabase;

namespace DevOpsMcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AzureDevOpsOptions>(configuration.GetSection(AzureDevOpsOptions.SectionName));
        services.Configure<EagleOptions>(configuration.GetSection(EagleOptions.SectionName));
        services.Configure<SesV2Options>(configuration.GetSection(SesV2Options.SectionName));
        services.Configure<EnhancedFeaturesOptions>(configuration.GetSection(EnhancedFeaturesOptions.SectionName));
        
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
        
        // Eagle Script Executor and Context Provider
        services.AddSingleton<IEagleSessionStore, EagleSessionStore>();
        services.AddSingleton<IMcpCallToolCommand, McpCallToolCommand>();
        services.AddSingleton<IMcpOutputCommand, McpOutputCommandHandler>();
        services.AddSingleton<ITclDictionaryConverter, TclDictionaryConverter>();
        services.AddSingleton<IEagleInterpreterPool, InterpreterPool>();
        services.AddSingleton<IEagleContextProvider, EagleContextProvider>();
        services.AddSingleton<IEagleOutputFormatter, EagleOutputFormatter>();
        services.AddSingleton<IEagleSecurityMonitor, EagleSecurityMonitor>();
        services.AddSingleton<IExecutionHistoryStore, ExecutionHistoryStore>();
        services.AddSingleton<IEagleScriptExecutor, EagleScriptExecutor>();
        
        // AWS SES V2 Client
        services.AddSingleton<IAmazonSimpleEmailServiceV2>(provider =>
        {
            var awsOptions = configuration.GetAWSOptions();
            
            // Use AWS SDK's default credential and region resolution
            // This supports IAM roles, environment variables, AWS profiles, etc.
            return awsOptions.CreateServiceClient<IAmazonSimpleEmailServiceV2>();
        });
        
        // Register email services
        services.AddSingleton<SesV2EmailSender>();
        services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<SesV2EmailSender>());
        services.AddSingleton<SesV2AccountService>();
        services.AddSingleton<IEmailAccountService>(provider => provider.GetRequiredService<SesV2AccountService>());
        
        // Enhanced Features - PostgreSQL/Supabase Integration
        var enhancedOptions = configuration.GetSection(EnhancedFeaturesOptions.SectionName).Get<EnhancedFeaturesOptions>();
        if (!string.IsNullOrEmpty(enhancedOptions?.DatabaseUrl))
        {
            // Add PostgreSQL with pgvector support
            services.AddDbContext<EnhancedFeaturesDbContext>(options =>
            {
                options.UseNpgsql(enhancedOptions.DatabaseUrl, npgsqlOptions =>
                {
                    npgsqlOptions.UseVector();
                });
            });
            
            // Add Supabase client for advanced features
            if (!string.IsNullOrEmpty(enhancedOptions.SupabaseUrl) && !string.IsNullOrEmpty(enhancedOptions.SupabaseKey))
            {
                services.AddSingleton<Supabase.Client>(provider =>
                {
                    var supabaseOptions = new SupabaseOptions
                    {
                        AutoRefreshToken = true,
                        AutoConnectRealtime = true
                    };
                    
                    return new Supabase.Client(enhancedOptions.SupabaseUrl, enhancedOptions.SupabaseKey, supabaseOptions);
                });
            }
            
            // Add enhanced feature repositories
            services.AddScoped<IEnhancedProjectRepository, EnhancedProjectRepository>();
            services.AddScoped<IEnhancedTaskRepository, EnhancedTaskRepository>();
            services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
            services.AddScoped<IDocumentVersionRepository, DocumentVersionRepository>();
        }
        
        return services;
    }
}