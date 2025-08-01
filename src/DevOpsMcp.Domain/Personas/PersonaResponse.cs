namespace DevOpsMcp.Domain.Personas;

public class PersonaResponse
{
    public string ResponseId { get; set; } = Guid.NewGuid().ToString();
    public string PersonaId { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public ResponseMetadata Metadata { get; set; } = new();
    public List<SuggestedAction> SuggestedActions { get; private set; } = new();
    public Dictionary<string, object> Context { get; private set; } = new();
    public PersonaConfidence Confidence { get; set; } = new();
    public bool IsError { get; set; }
}

public class ResponseMetadata
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ResponseType { get; set; } = "Standard";
    public string Tone { get; set; } = "Professional";
    public int ComplexityLevel { get; set; } = 2;
    public List<string> Topics { get; private set; } = new();
    public Dictionary<string, string> References { get; private set; } = new();
    public string IntentClassification { get; set; } = string.Empty;
}

public class SuggestedAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ActionPriority Priority { get; set; }
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; private set; } = new();
    public List<string> Prerequisites { get; private set; } = new();
    public EstimatedImpact Impact { get; set; } = new();
}

public enum ActionPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class EstimatedImpact
{
    public string TimeToComplete { get; set; } = "Unknown";
    public string EffortLevel { get; set; } = "Medium";
    public List<string> AffectedSystems { get; private set; } = new();
    public Dictionary<string, double> Metrics { get; private set; } = new();
}

public class PersonaConfidence
{
    public double Overall { get; set; }
    public double DomainExpertise { get; set; }
    public double ContextRelevance { get; set; }
    public double ResponseQuality { get; set; }
    public List<string> Caveats { get; private set; } = new();
}