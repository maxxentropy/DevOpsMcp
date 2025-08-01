namespace DevOpsMcp.Domain.Personas;

public class DevOpsContext
{
    public ProjectMetadata Project { get; set; } = new();
    public UserProfile User { get; set; } = new();
    public EnvironmentContext Environment { get; set; } = new();
    public ComplianceRequirements Compliance { get; set; } = new();
    public PerformanceConstraints Performance { get; set; } = new();
    public SecurityContext Security { get; set; } = new();
    public TeamDynamics Team { get; set; } = new();
    public TechnologyConfiguration TechStack { get; set; } = new();
    public SessionContext? Session { get; set; }
}

public class ProjectMetadata
{
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Stage { get; set; } = "Development";
    public string Type { get; set; } = "Standard";
    public string Priority { get; set; } = "Medium";
    public string Methodology { get; set; } = "Agile";
    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public Dictionary<string, string> Tags { get; private set; } = new();
    public List<ProjectConstraint> Constraints { get; private set; } = new();
}

public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = "Intermediate";
    public ExperienceLevel Experience { get; set; }
    public List<string> Specializations { get; private set; } = new();
    public string Specialization => Specializations.FirstOrDefault() ?? string.Empty;
    public CommunicationPreferences Communication { get; set; } = new();
    public LearningStyle LearningStyle { get; set; }
    public RiskProfile RiskProfile { get; set; }
    public PreferredTools PreferredTools { get; set; } = new();
    public string TimeZone { get; set; } = "UTC";
    public TeamRole TeamRole { get; set; }
}

public class EnvironmentContext
{
    public string EnvironmentType { get; set; } = "Development";
    public List<string> Regions { get; private set; } = new();
    public Dictionary<string, string> Resources { get; private set; } = new();
    public bool IsProduction { get; set; }
    public bool IsRegulated { get; set; }
    public bool IsExternallyAccessible { get; set; }
}

public class ComplianceRequirements
{
    public List<string> Standards { get; private set; } = new();
    public List<string> Certifications { get; private set; } = new();
    public List<string> RequiredFrameworks { get; private set; } = new();
    public bool RequiresAudit { get; set; }
    public string DataClassification { get; set; } = "Internal";
}

public class PerformanceConstraints
{
    public int MaxResponseTimeMs { get; set; } = 5000;
    public int MaxMemoryMb { get; set; } = 1024;
    public int MaxCpuPercent { get; set; } = 80;
    public int MaxConcurrentRequests { get; set; } = 100;
}

public class SecurityContext
{
    public string SecurityLevel { get; set; } = "Standard";
    public string ThreatLevel { get; set; } = "Low";
    public List<string> RequiredPermissions { get; private set; } = new();
    public List<string> SecurityToolsInUse { get; private set; } = new();
    public bool RequiresMfa { get; set; }
    public bool MfaEnabled { get; set; }
    public bool RequiresEncryption { get; set; } = true;
    public int RecentIncidents { get; set; }
    public DateTime? LastSecurityScan { get; set; }
    public bool UnknownAssets { get; set; }
}

public class TeamDynamics
{
    public int TeamSize { get; set; }
    public string TeamMaturity { get; set; } = "Developing";
    public string MaturityLevel { get; set; } = "Forming";
    public List<string> TeamCapabilities { get; private set; } = new();
    public string CollaborationStyle { get; set; } = "Agile";
    public bool HasCriticalIssues { get; set; }
    public double CurrentUtilization { get; set; } = 75.0;
    public double AutomationLevel { get; set; } = 50.0;
}

public class TechnologyConfiguration
{
    public List<string> Languages { get; private set; } = new();
    public List<string> Frameworks { get; private set; } = new();
    public List<string> RequiredFrameworks { get; private set; } = new();
    public List<string> Tools { get; private set; } = new();
    public string CloudProvider { get; set; } = "Azure";
    public string CiCdPlatform { get; set; } = "Azure DevOps";
}

public enum ExperienceLevel
{
    Junior,
    MidLevel,
    Senior,
    Lead,
    Principal
}

public class CommunicationPreferences
{
    public string PreferredLanguage { get; set; } = "English";
    public string DetailLevel { get; set; } = "Balanced";
    public bool PreferVisuals { get; set; }
    public bool PreferExamples { get; set; } = true;
}

public enum LearningStyle
{
    Visual,
    Auditory,
    Kinesthetic,
    ReadingWriting
}

public enum RiskProfile
{
    RiskAverse,
    Balanced,
    RiskTolerant
}

public class PreferredTools
{
    public List<string> Ide { get; private set; } = new();
    public List<string> VersionControl { get; private set; } = new();
    public List<string> Monitoring { get; private set; } = new();
    public List<string> Communication { get; private set; } = new();
}

public enum TeamRole
{
    Developer,
    Lead,
    Manager,
    Architect,
    DevOps,
    QA,
    Security,
    Executive
}

public class ProjectConstraint
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
}

public class SessionContext
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public int InteractionCount { get; set; }
    public Dictionary<string, object> SessionData { get; private set; } = new();
}

