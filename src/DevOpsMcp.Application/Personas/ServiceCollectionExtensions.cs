using DevOpsMcp.Application.Personas;
using DevOpsMcp.Application.Personas.Adaptation;
using DevOpsMcp.Application.Personas.Memory;
using DevOpsMcp.Application.Personas.Orchestration;
using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Adaptation;
using DevOpsMcp.Domain.Personas.Orchestration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class PersonaServiceCollectionExtensions
{
    public static IServiceCollection AddPersonaServices(this IServiceCollection services)
    {
        // Core persona services
        services.AddScoped<IPersonaMemoryManager, PersonaMemoryManager>();
        services.AddSingleton<IPersonaOrchestrator, PersonaOrchestrator>();
        services.AddScoped<IPersonaBehaviorAdapter, PersonaBehaviorAdapter>();
        services.AddSingleton<IPersonaLearningEngine, PersonaLearningEngine>();

        // Register individual personas
        services.AddScoped<DevOpsEngineerPersona>();
        services.AddScoped<SiteReliabilityEngineerPersona>();
        services.AddScoped<SecurityEngineerPersona>();
        services.AddScoped<EngineeringManagerPersona>();

        // Register personas as IDevOpsPersona
        services.AddScoped<IDevOpsPersona, DevOpsEngineerPersona>(provider => provider.GetRequiredService<DevOpsEngineerPersona>());
        services.AddScoped<IDevOpsPersona, SiteReliabilityEngineerPersona>(provider => provider.GetRequiredService<SiteReliabilityEngineerPersona>());
        services.AddScoped<IDevOpsPersona, SecurityEngineerPersona>(provider => provider.GetRequiredService<SecurityEngineerPersona>());
        services.AddScoped<IDevOpsPersona, EngineeringManagerPersona>(provider => provider.GetRequiredService<EngineeringManagerPersona>());

        return services;
    }

    public static IServiceCollection AddPersonaInfrastructure(this IServiceCollection services, Action<PersonaInfrastructureOptions>? configure = null)
    {
        var options = new PersonaInfrastructureOptions();
        configure?.Invoke(options);

        // Add distributed cache if not already registered
        if (!services.Any(s => s.ServiceType == typeof(IDistributedCache)))
        {
            services.AddDistributedMemoryCache();
        }

        // Add memory store based on configuration
        if (options.UseFileBasedStorage)
        {
            // Infrastructure-specific registrations should be done by the host application
            throw new NotImplementedException($"Please register IPersonaMemoryStore implementation in your host application. File-based storage path would be: {options.FileStorageBasePath}");
        }
        else
        {
            // Add other storage implementations here (e.g., database, cloud storage)
            throw new NotImplementedException("Only file-based storage is currently implemented");
        }
    }
}

public class PersonaInfrastructureOptions
{
    public bool UseFileBasedStorage { get; set; } = true;
    public string FileStorageBasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DevOpsMcp",
        "PersonaMemory"
    );
    public int MaxContextsPerPersona { get; set; } = 100;
}