namespace DevOpsMcp.Domain.Personas;

public class DevOpsTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskCategory Category { get; set; }
    public TaskComplexity Complexity { get; set; }
    public List<string> RequiredSkills { get; private set; } = new();
    public Dictionary<string, object> Parameters { get; private set; } = new();
    public TaskConstraints Constraints { get; private set; } = new();
    public List<string> Dependencies { get; private set; } = new();
}

public enum TaskCategory
{
    Infrastructure,
    Deployment,
    Monitoring,
    Security,
    Performance,
    Troubleshooting,
    Architecture,
    Planning,
    Documentation,
    Automation
}

public enum TaskComplexity
{
    Trivial,
    Simple,
    Moderate,
    Complex,
    Expert
}

public class TaskConstraints
{
    public TimeSpan? TimeLimit { get; set; }
    public List<string> TechnologyConstraints { get; private set; } = new();
    public Dictionary<string, string> EnvironmentConstraints { get; private set; } = new();
    public bool RequiresApproval { get; set; }
    public string RiskLevel { get; set; } = "Low";
}

public enum DevOpsSpecialization
{
    Infrastructure,
    Development,
    Security,
    Reliability,
    Architecture,
    Management,
    Quality,
    Observability
}

public class ProjectContext
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Stage { get; set; } = "Development";
    public string CurrentPhase { get; set; } = "Development";
    public Dictionary<string, object> ProjectState { get; private set; } = new();
    public List<string> ActiveWorkstreams { get; private set; } = new();
    public Dictionary<string, string> TeamMembers { get; private set; } = new();
    public List<ProjectConstraint> Constraints { get; private set; } = new();
    public TechnologyConfiguration? TechnologyStack { get; set; }
    public Dictionary<string, object> ContextData { get; private set; } = new();
}