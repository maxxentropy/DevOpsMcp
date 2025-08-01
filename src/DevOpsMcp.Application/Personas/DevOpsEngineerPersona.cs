using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Logging;
using MediatR;

namespace DevOpsMcp.Application.Personas;

public class DevOpsEngineerPersona : BaseDevOpsPersona
{
    private readonly IMediator _mediator;
    private const string PersonaId = "devops-engineer";
    
    public DevOpsEngineerPersona(
        ILogger<DevOpsEngineerPersona> logger,
        IPersonaMemoryManager memoryManager,
        IMediator mediator) 
        : base(logger, memoryManager)
    {
        _mediator = mediator;
        Initialize();
    }

    public override string Id => PersonaId;
    public override string Name => "DevOps Engineer";
    public override string Role => "DevOps and CI/CD Specialist";
    public override string Description => "Expert in CI/CD pipelines, deployment automation, infrastructure as code, and DevOps best practices";
    public override DevOpsSpecialization Specialization => DevOpsSpecialization.Development;

    protected override PersonaConfiguration GetDefaultConfiguration()
    {
        return new PersonaConfiguration
        {
            CommunicationStyle = CommunicationStyle.TechnicalPrecise,
            TechnicalDepth = TechnicalDepth.Advanced,
            RiskTolerance = RiskTolerance.Moderate,
            DecisionMakingStyle = DecisionMakingStyle.DataDriven,
            CollaborationPreferences = CollaborationPreferences.CrossFunctional,
            SecurityPosture = SecurityPosture.Standard
        };
    }

    protected override Dictionary<string, object> InitializeCapabilities()
    {
        return new Dictionary<string, object>
        {
            ["ci_cd_pipelines"] = new List<string> { "Azure Pipelines", "GitHub Actions", "Jenkins", "GitLab CI" },
            ["deployment_strategies"] = new List<string> { "Blue-Green", "Canary", "Rolling", "Feature Flags" },
            ["infrastructure_as_code"] = new List<string> { "Terraform", "ARM Templates", "Bicep", "Pulumi" },
            ["containerization"] = new List<string> { "Docker", "Kubernetes", "Helm", "Docker Compose" },
            ["automation_tools"] = new List<string> { "Ansible", "PowerShell", "Bash", "Python" },
            ["monitoring"] = new List<string> { "Prometheus", "Grafana", "Azure Monitor", "ELK Stack" },
            ["version_control"] = new List<string> { "Git", "Azure Repos", "GitHub", "GitLab" },
            ["cloud_platforms"] = new List<string> { "Azure", "AWS", "GCP" }
        };
    }

    protected override async Task<RequestAnalysis> AnalyzeRequestAsync(string request, DevOpsContext context)
    {
        var analysis = new RequestAnalysis
        {
            Intent = DetermineDevOpsIntent(request),
            Urgency = CalculateUrgency(request, context),
            EstimatedCategory = EstimateTaskCategory(request)
        };

        // Add topics
        foreach (var topic in ExtractDevOpsTopics(request))
        {
            analysis.Topics.Add(topic);
        }
        
        // Add context
        analysis.Context["environment"] = context.Environment.EnvironmentType;
        analysis.Context["project_phase"] = context.Project.Stage;
        analysis.Context["tech_stack"] = string.Join(",", context.TechStack.Tools);

        // Extract entities specific to DevOps
        var entities = ExtractDevOpsEntities(request);
        foreach (var entity in entities)
        {
            analysis.Entities[entity.Key] = entity.Value;
        }

        return await Task.FromResult(analysis);
    }

    protected override async Task<PersonaResponse> GenerateResponseAsync(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Metadata = new ResponseMetadata
            {
                ResponseType = "DevOps Guidance",
            }
        };

        // Generate response based on intent
        switch (analysis.Intent)
        {
            case "Pipeline Configuration":
                response = await GeneratePipelineResponse(analysis, context);
                break;
            case "Deployment Strategy":
                response = await GenerateDeploymentResponse(analysis, context);
                break;
            case "Infrastructure Setup":
                response = await GenerateInfrastructureResponse(analysis, context);
                break;
            case "Troubleshooting":
                response = await GenerateTroubleshootingResponse(analysis, context);
                break;
            case "Best Practices":
                response = await GenerateBestPracticesResponse(analysis, context);
                break;
            default:
                response = await GenerateGeneralDevOpsResponse(analysis, context);
                break;
        }

        // Add topics to response
        foreach (var topic in analysis.Topics)
        {
            response.Metadata.Topics.Add(topic);
        }
        
        // Add suggested actions based on context
        foreach (var action in GenerateSuggestedActions(analysis, context))
        {
            response.SuggestedActions.Add(action);
        }
        
        // Calculate confidence
        response.Confidence = CalculateResponseConfidence(analysis, context);

        return response;
    }

    protected override Dictionary<string, double> GetAlignmentWeights()
    {
        return new Dictionary<string, double>
        {
            ["category"] = 0.3,
            ["skills"] = 0.4,
            ["complexity"] = 0.2,
            ["specialization"] = 0.1
        };
    }

    protected override Dictionary<TaskCategory, double> GetCategoryAlignmentMap()
    {
        return new Dictionary<TaskCategory, double>
        {
            [TaskCategory.Deployment] = 1.0,
            [TaskCategory.Automation] = 1.0,
            [TaskCategory.Infrastructure] = 0.8,
            [TaskCategory.Monitoring] = 0.7,
            [TaskCategory.Performance] = 0.6,
            [TaskCategory.Security] = 0.5,
            [TaskCategory.Troubleshooting] = 0.7,
            [TaskCategory.Architecture] = 0.6,
            [TaskCategory.Planning] = 0.5,
            [TaskCategory.Documentation] = 0.4
        };
    }

    protected override List<string> GetPersonaSkills()
    {
        return new List<string>
        {
            "CI/CD", "Pipeline Design", "Deployment Automation", "Infrastructure as Code",
            "Containerization", "Kubernetes", "Docker", "Git", "Azure DevOps",
            "Monitoring", "Logging", "Scripting", "Cloud Platforms", "DevOps Tools",
            "Build Automation", "Release Management", "Configuration Management"
        };
    }

    private string DetermineDevOpsIntent(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("pipeline") || requestLower.Contains("ci/cd") || requestLower.Contains("build"))
            return "Pipeline Configuration";
        if (requestLower.Contains("deploy") || requestLower.Contains("release"))
            return "Deployment Strategy";
        if (requestLower.Contains("infrastructure") || requestLower.Contains("terraform") || requestLower.Contains("iac"))
            return "Infrastructure Setup";
        if (requestLower.Contains("error") || requestLower.Contains("fail") || requestLower.Contains("issue"))
            return "Troubleshooting";
        if (requestLower.Contains("best practice") || requestLower.Contains("recommend"))
            return "Best Practices";

        return "General DevOps";
    }

    private List<string> ExtractDevOpsTopics(string request)
    {
        var topics = new List<string>();
        var requestLower = request.ToLowerInvariant();

        var topicKeywords = new Dictionary<string, string[]>
        {
            ["CI/CD"] = new[] { "pipeline", "ci/cd", "continuous integration", "continuous deployment" },
            ["Containerization"] = new[] { "docker", "container", "kubernetes", "k8s" },
            ["Infrastructure"] = new[] { "infrastructure", "terraform", "arm", "bicep", "iac" },
            ["Monitoring"] = new[] { "monitor", "logging", "metrics", "observability" },
            ["Security"] = new[] { "security", "secrets", "credentials", "vulnerability" },
            ["Automation"] = new[] { "automate", "automation", "script", "workflow" }
        };

        foreach (var (topic, keywords) in topicKeywords)
        {
            if (keywords.Any(keyword => requestLower.Contains(keyword)))
            {
                topics.Add(topic);
            }
        }

        return topics;
    }

    private double CalculateUrgency(string request, DevOpsContext context)
    {
        var urgency = 0.5; // Default medium urgency

        // Check for urgency indicators
        if (request.ToLowerInvariant().Contains("urgent") || request.ToLowerInvariant().Contains("asap"))
            urgency += 0.3;

        // Production environment increases urgency
        if (context.Environment.IsProduction)
            urgency += 0.2;

        // Check for critical keywords
        if (request.ToLowerInvariant().Contains("down") || request.ToLowerInvariant().Contains("failed"))
            urgency += 0.3;

        return Math.Min(1.0, urgency);
    }

    private TaskCategory EstimateTaskCategory(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("deploy")) return TaskCategory.Deployment;
        if (requestLower.Contains("monitor")) return TaskCategory.Monitoring;
        if (requestLower.Contains("automat")) return TaskCategory.Automation;
        if (requestLower.Contains("infrastructure")) return TaskCategory.Infrastructure;
        if (requestLower.Contains("performance")) return TaskCategory.Performance;
        if (requestLower.Contains("security")) return TaskCategory.Security;

        return TaskCategory.Infrastructure;
    }

    private Dictionary<string, object> ExtractDevOpsEntities(string request)
    {
        var entities = new Dictionary<string, object>();

        // Extract technology mentions
        var technologies = new List<string>();
        var techKeywords = new[] { "azure", "docker", "kubernetes", "terraform", "jenkins", "github", "git" };
        foreach (var tech in techKeywords)
        {
            if (request.ToLowerInvariant().Contains(tech))
                technologies.Add(tech);
        }
        if (technologies.Any())
            entities["technologies"] = technologies;

        // Extract environment mentions
        var environments = new[] { "production", "staging", "development", "test", "qa" };
        var mentionedEnvs = environments.Where(env => request.ToLowerInvariant().Contains(env)).ToList();
        if (mentionedEnvs.Any())
            entities["environments"] = mentionedEnvs;

        return entities;
    }

    private async Task<PersonaResponse> GeneratePipelineResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GeneratePipelineGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Pipeline Configuration"
            }
        };
        
        response.Metadata.Topics.Add("CI/CD");
        response.Metadata.Topics.Add("Pipeline Design");

        // Add pipeline-specific suggested actions
        response.SuggestedActions.Add(new SuggestedAction
        {
            Title = "Review Pipeline Configuration",
            Description = "Analyze current pipeline setup for optimization opportunities",
            Priority = ActionPriority.High,
            Category = "Pipeline"
        });

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateDeploymentResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateDeploymentGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Deployment Strategy"
            }
        };
        
        response.Metadata.Topics.Add("Deployment");
        response.Metadata.Topics.Add("Release Management");

        // Determine appropriate deployment strategy
        var strategy = DetermineDeploymentStrategy(context);
        response.Context["recommended_strategy"] = strategy;

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateInfrastructureResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateInfrastructureGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Infrastructure Guidance"
            }
        };
        
        response.Metadata.Topics.Add("Infrastructure as Code");
        response.Metadata.Topics.Add("Cloud Resources");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateTroubleshootingResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateTroubleshootingSteps(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Troubleshooting"
            }
        };
        
        response.Metadata.Topics.Add("Problem Solving");
        response.Metadata.Topics.Add("Diagnostics");

        // Add diagnostic actions
        response.SuggestedActions.Add(new SuggestedAction
        {
            Title = "Run Diagnostic Checks",
            Description = "Execute standard diagnostic procedures",
            Priority = ActionPriority.Critical,
            Category = "Troubleshooting"
        });

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateBestPracticesResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateBestPracticesGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Best Practices"
            }
        };
        
        response.Metadata.Topics.Add("DevOps Best Practices");
        response.Metadata.Topics.Add("Guidelines");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateGeneralDevOpsResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = "I'm here to help with your DevOps needs. Could you provide more specific details about what you're trying to achieve?",
            Metadata = new ResponseMetadata
            {
                ResponseType = "General Guidance"
            }
        };
        
        // Topics already added in main method from analysis.Topics

        return await Task.FromResult(response);
    }

    private string GeneratePipelineGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Based on your pipeline requirements, here's my recommendation:");
        guidance.AppendLine();
        
        if (context.TechStack.CiCdPlatform == "Azure DevOps")
        {
            guidance.AppendLine("For Azure DevOps pipelines:");
            guidance.AppendLine("1. Use YAML-based pipelines for version control and reusability");
            guidance.AppendLine("2. Implement stage gates for production deployments");
            guidance.AppendLine("3. Utilize variable groups for environment-specific configurations");
            guidance.AppendLine("4. Enable pipeline caching to improve build times");
        }
        
        if (Configuration.TechnicalDepth >= TechnicalDepth.Advanced)
        {
            guidance.AppendLine();
            guidance.AppendLine("Advanced considerations:");
            guidance.AppendLine("- Implement parallel jobs for faster execution");
            guidance.AppendLine("- Use deployment slots for zero-downtime deployments");
            guidance.AppendLine("- Configure approval gates for production stages");
        }
        
        return guidance.ToString();
    }

    private string GenerateDeploymentGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var strategy = DetermineDeploymentStrategy(context);
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine($"For your {context.Environment.EnvironmentType} environment, I recommend a {strategy} deployment strategy.");
        guidance.AppendLine();
        
        switch (strategy)
        {
            case "Blue-Green":
                guidance.AppendLine("Blue-Green Deployment Steps:");
                guidance.AppendLine("1. Provision identical production environment (Green)");
                guidance.AppendLine("2. Deploy new version to Green environment");
                guidance.AppendLine("3. Run smoke tests on Green");
                guidance.AppendLine("4. Switch traffic from Blue to Green");
                guidance.AppendLine("5. Keep Blue as rollback option");
                break;
            case "Canary":
                guidance.AppendLine("Canary Deployment Steps:");
                guidance.AppendLine("1. Deploy to small subset of servers/users");
                guidance.AppendLine("2. Monitor metrics and error rates");
                guidance.AppendLine("3. Gradually increase traffic percentage");
                guidance.AppendLine("4. Full rollout if metrics are healthy");
                break;
            case "Rolling":
                guidance.AppendLine("Rolling Deployment Steps:");
                guidance.AppendLine("1. Deploy to one server at a time");
                guidance.AppendLine("2. Health check before proceeding");
                guidance.AppendLine("3. Maintain service availability");
                guidance.AppendLine("4. Rollback if issues detected");
                break;
        }
        
        return guidance.ToString();
    }

    private string GenerateInfrastructureGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Infrastructure as Code recommendations:");
        guidance.AppendLine();
        
        if (context.TechStack.CloudProvider == "Azure")
        {
            guidance.AppendLine("For Azure infrastructure:");
            guidance.AppendLine("- Use Bicep or Terraform for declarative infrastructure");
            guidance.AppendLine("- Implement resource tagging strategy");
            guidance.AppendLine("- Configure diagnostic settings for all resources");
            guidance.AppendLine("- Use Azure Policy for governance");
        }
        
        if (Configuration.SecurityPosture >= SecurityPosture.Standard)
        {
            guidance.AppendLine();
            guidance.AppendLine("Security considerations:");
            guidance.AppendLine("- Store secrets in Azure Key Vault");
            guidance.AppendLine("- Enable managed identities where possible");
            guidance.AppendLine("- Implement network segmentation");
            guidance.AppendLine("- Enable Azure Defender for enhanced security");
        }
        
        return guidance.ToString();
    }

    private string GenerateTroubleshootingSteps(RequestAnalysis analysis, DevOpsContext context)
    {
        var steps = new System.Text.StringBuilder();
        
        steps.AppendLine("Let me help you troubleshoot this issue. Here's a systematic approach:");
        steps.AppendLine();
        steps.AppendLine("1. **Check Recent Changes**");
        steps.AppendLine("   - Review recent deployments or configuration changes");
        steps.AppendLine("   - Check commit history for relevant changes");
        steps.AppendLine();
        steps.AppendLine("2. **Examine Logs**");
        steps.AppendLine("   - Application logs for errors or warnings");
        steps.AppendLine("   - Infrastructure logs for resource issues");
        steps.AppendLine("   - Pipeline logs if deployment-related");
        steps.AppendLine();
        steps.AppendLine("3. **Verify Resources**");
        steps.AppendLine("   - Check resource health and availability");
        steps.AppendLine("   - Verify network connectivity");
        steps.AppendLine("   - Confirm proper permissions");
        steps.AppendLine();
        steps.AppendLine("4. **Test in Isolation**");
        steps.AppendLine("   - Reproduce in lower environment if possible");
        steps.AppendLine("   - Test individual components");
        
        return steps.ToString();
    }

    private string GenerateBestPracticesGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var practices = new System.Text.StringBuilder();
        
        practices.AppendLine("DevOps Best Practices for your scenario:");
        practices.AppendLine();
        practices.AppendLine("**Version Control**");
        practices.AppendLine("- Use GitFlow or GitHub Flow branching strategy");
        practices.AppendLine("- Implement branch protection rules");
        practices.AppendLine("- Require code reviews for all changes");
        practices.AppendLine();
        practices.AppendLine("**CI/CD Pipeline**");
        practices.AppendLine("- Automate all builds and deployments");
        practices.AppendLine("- Include automated testing at every stage");
        practices.AppendLine("- Implement quality gates");
        practices.AppendLine();
        practices.AppendLine("**Infrastructure**");
        practices.AppendLine("- Everything as code (IaC)");
        practices.AppendLine("- Immutable infrastructure");
        practices.AppendLine("- Environment parity");
        practices.AppendLine();
        practices.AppendLine("**Monitoring**");
        practices.AppendLine("- Implement comprehensive logging");
        practices.AppendLine("- Set up proactive alerts");
        practices.AppendLine("- Track key performance indicators");
        
        return practices.ToString();
    }

    private string DetermineDeploymentStrategy(DevOpsContext context)
    {
        if (context.Environment.IsProduction && context.Performance.MaxConcurrentRequests > 1000)
            return "Blue-Green";
        
        if (context.Environment.IsProduction && Configuration.RiskTolerance == RiskTolerance.Conservative)
            return "Canary";
        
        return "Rolling";
    }

    private List<SuggestedAction> GenerateSuggestedActions(RequestAnalysis analysis, DevOpsContext context)
    {
        var actions = new List<SuggestedAction>();

        if (analysis.Intent == "Pipeline Configuration")
        {
            actions.Add(new SuggestedAction
            {
                Title = "Optimize Pipeline Performance",
                Description = "Review and optimize pipeline execution time",
                Priority = ActionPriority.Medium,
                Category = "Optimization"
            });
        }

        if (context.Environment.IsProduction)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Implement Monitoring",
                Description = "Ensure comprehensive monitoring is in place",
                Priority = ActionPriority.High,
                Category = "Monitoring"
            });
        }

        if (analysis.Urgency > 0.7)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Create Incident Response Plan",
                Description = "Document steps for handling similar issues",
                Priority = ActionPriority.High,
                Category = "Documentation"
            });
        }

        return actions;
    }

    private PersonaConfidence CalculateResponseConfidence(RequestAnalysis analysis, DevOpsContext context)
    {
        var confidence = new PersonaConfidence
        {
            DomainExpertise = 0.9, // DevOps is our specialty
            ContextRelevance = CalculateContextRelevance(analysis, context),
            ResponseQuality = 0.85
        };

        // Calculate overall confidence
        confidence.Overall = (confidence.DomainExpertise * 0.4 + 
                            confidence.ContextRelevance * 0.3 + 
                            confidence.ResponseQuality * 0.3);

        // Add caveats if needed
        if (context.Environment.IsProduction && context.Environment.IsRegulated)
        {
            confidence.Caveats.Add("Additional compliance validation may be required");
        }

        if (analysis.Urgency > 0.8)
        {
            confidence.Caveats.Add("Expedited response - follow up with detailed analysis");
        }

        return confidence;
    }

    private double CalculateContextRelevance(RequestAnalysis analysis, DevOpsContext context)
    {
        var relevance = 0.5;

        // Check if we have experience with the tech stack
        if (context.TechStack.CiCdPlatform == "Azure DevOps")
            relevance += 0.2;

        // Check if the request aligns with our capabilities
        if (analysis.Topics.Any(t => Capabilities.ContainsKey(t.ToLowerInvariant().Replace(" ", "_"))))
            relevance += 0.2;

        // Environment familiarity
        if (context.TechStack.CloudProvider == "Azure")
            relevance += 0.1;

        return Math.Min(1.0, relevance);
    }
}