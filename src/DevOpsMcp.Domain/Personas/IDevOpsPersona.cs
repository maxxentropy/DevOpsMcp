namespace DevOpsMcp.Domain.Personas;

public interface IDevOpsPersona
{
    string Id { get; }
    string Name { get; }
    string Role { get; }
    string Description { get; }
    DevOpsSpecialization Specialization { get; }
    Dictionary<string, object> Capabilities { get; }
    PersonaConfiguration Configuration { get; }
    
    Task<PersonaResponse> ProcessRequestAsync(DevOpsContext context, string request);
    Task<double> CalculateRoleAlignmentAsync(DevOpsTask task);
    Task AdaptBehaviorAsync(UserProfile userProfile, ProjectContext projectContext);
}