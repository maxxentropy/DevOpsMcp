using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Logging;
using MediatR;

namespace DevOpsMcp.Application.Personas;

public class EngineeringManagerPersona : BaseDevOpsPersona
{
    private readonly IMediator _mediator;
    private const string PersonaId = "engineering-manager";
    
    public EngineeringManagerPersona(
        ILogger<EngineeringManagerPersona> logger,
        IPersonaMemoryManager memoryManager,
        IMediator mediator) 
        : base(logger, memoryManager)
    {
        _mediator = mediator;
        Initialize();
    }

    public override string Id => PersonaId;
    public override string Name => "Engineering Manager";
    public override string Role => "Technical Leadership and Team Management";
    public override string Description => "Expert in team leadership, project management, strategic planning, stakeholder communication, and engineering excellence";
    public override DevOpsSpecialization Specialization => DevOpsSpecialization.Management;

    protected override PersonaConfiguration GetDefaultConfiguration()
    {
        return new PersonaConfiguration
        {
            CommunicationStyle = CommunicationStyle.BusinessOriented,
            TechnicalDepth = TechnicalDepth.Intermediate,
            RiskTolerance = RiskTolerance.Moderate,
            DecisionMakingStyle = DecisionMakingStyle.Consensus,
            CollaborationPreferences = CollaborationPreferences.Leadership,
            SecurityPosture = SecurityPosture.Standard
        };
    }

    protected override Dictionary<string, object> InitializeCapabilities()
    {
        return new Dictionary<string, object>
        {
            ["management_skills"] = new List<string> { "Team Building", "Performance Management", "Conflict Resolution", "Coaching", "Hiring" },
            ["project_management"] = new List<string> { "Agile", "Scrum", "Kanban", "SAFe", "Waterfall", "Risk Management" },
            ["strategic_planning"] = new List<string> { "Roadmap Planning", "Resource Allocation", "Budget Management", "OKRs", "KPIs" },
            ["technical_oversight"] = new List<string> { "Architecture Review", "Code Review", "Technical Debt", "Technology Selection" },
            ["stakeholder_management"] = new List<string> { "Executive Communication", "Cross-team Collaboration", "Vendor Management" },
            ["process_improvement"] = new List<string> { "SDLC Optimization", "DevOps Transformation", "Metrics & Analytics", "Automation" },
            ["compliance_governance"] = new List<string> { "Policy Development", "Audit Preparation", "Risk Assessment", "Compliance Tracking" },
            ["team_development"] = new List<string> { "Career Development", "Training Programs", "Mentorship", "Succession Planning" }
        };
    }

    protected override async Task<RequestAnalysis> AnalyzeRequestAsync(string request, DevOpsContext context)
    {
        var analysis = new RequestAnalysis
        {
            Intent = DetermineManagementIntent(request),
            Urgency = CalculateManagementUrgency(request, context),
            EstimatedCategory = DetermineTaskCategory(request)
        };

        // Add topics
        foreach (var topic in ExtractManagementTopics(request))
        {
            analysis.Topics.Add(topic);
        }
        
        // Add context
        analysis.Context["team_size"] = context.Team.TeamSize.ToString();
        analysis.Context["project_phase"] = context.Project.Stage;
        analysis.Context["team_maturity"] = context.Team.MaturityLevel;
        analysis.Context["business_impact"] = AssessBusinessImpact(context);

        // Extract entities
        var entities = ExtractManagementEntities(request);
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
                ResponseType = "Management Guidance",
            }
        };

        switch (analysis.Intent)
        {
            case "Team Management":
                response = await GenerateTeamManagementResponse(analysis, context);
                break;
            case "Project Planning":
                response = await GenerateProjectPlanningResponse(analysis, context);
                break;
            case "Resource Allocation":
                response = await GenerateResourceResponse(analysis, context);
                break;
            case "Performance Review":
                response = await GeneratePerformanceResponse(analysis, context);
                break;
            case "Technical Decision":
                response = await GenerateTechnicalDecisionResponse(analysis, context);
                break;
            case "Process Improvement":
                response = await GenerateProcessResponse(analysis, context);
                break;
            case "Stakeholder Communication":
                response = await GenerateStakeholderResponse(analysis, context);
                break;
            default:
                response = await GenerateGeneralManagementResponse(analysis, context);
                break;
        }

        // Add topics to response
        foreach (var topic in analysis.Topics)
        {
            response.Metadata.Topics.Add(topic);
        }
        
        // Add suggested actions
        foreach (var action in GenerateManagementActions(analysis, context))
        {
            response.SuggestedActions.Add(action);
        }
        
        response.Confidence = CalculateResponseConfidence(analysis, context);

        return response;
    }

    protected override Dictionary<string, double> GetAlignmentWeights()
    {
        return new Dictionary<string, double>
        {
            ["category"] = 0.3,
            ["skills"] = 0.3,
            ["complexity"] = 0.2,
            ["specialization"] = 0.2
        };
    }

    protected override Dictionary<TaskCategory, double> GetCategoryAlignmentMap()
    {
        return new Dictionary<TaskCategory, double>
        {
            [TaskCategory.Planning] = 1.0,
            [TaskCategory.Architecture] = 0.7,
            [TaskCategory.Documentation] = 0.8,
            [TaskCategory.Troubleshooting] = 0.5,
            [TaskCategory.Performance] = 0.6,
            [TaskCategory.Security] = 0.6,
            [TaskCategory.Monitoring] = 0.5,
            [TaskCategory.Deployment] = 0.5,
            [TaskCategory.Automation] = 0.6,
            [TaskCategory.Infrastructure] = 0.4
        };
    }

    protected override List<string> GetPersonaSkills()
    {
        return new List<string>
        {
            "Leadership", "Team Management", "Project Management", "Strategic Planning",
            "Communication", "Conflict Resolution", "Budget Management", "Risk Management",
            "Agile Methodologies", "Performance Management", "Stakeholder Management",
            "Technical Oversight", "Process Improvement", "Change Management", "Mentoring"
        };
    }

    private string DetermineManagementIntent(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("team") || requestLower.Contains("people") || requestLower.Contains("hire"))
            return "Team Management";
        if (requestLower.Contains("project") || requestLower.Contains("timeline") || requestLower.Contains("deadline"))
            return "Project Planning";
        if (requestLower.Contains("resource") || requestLower.Contains("budget") || requestLower.Contains("allocat"))
            return "Resource Allocation";
        if (requestLower.Contains("performance") || requestLower.Contains("review") || requestLower.Contains("feedback"))
            return "Performance Review";
        if (requestLower.Contains("technical") || requestLower.Contains("architect") || requestLower.Contains("technology"))
            return "Technical Decision";
        if (requestLower.Contains("process") || requestLower.Contains("improve") || requestLower.Contains("optimize"))
            return "Process Improvement";
        if (requestLower.Contains("stakeholder") || requestLower.Contains("executive") || requestLower.Contains("communicate"))
            return "Stakeholder Communication";

        return "General Management";
    }

    private List<string> ExtractManagementTopics(string request)
    {
        var topics = new List<string>();
        var requestLower = request.ToLowerInvariant();

        var topicKeywords = new Dictionary<string, string[]>
        {
            ["Team Leadership"] = new[] { "team", "leadership", "manage", "lead", "supervise" },
            ["Project Management"] = new[] { "project", "sprint", "milestone", "delivery", "roadmap" },
            ["Resource Planning"] = new[] { "resource", "capacity", "budget", "allocation", "staffing" },
            ["Performance Management"] = new[] { "performance", "review", "feedback", "evaluation", "goals" },
            ["Technical Strategy"] = new[] { "technical", "architecture", "technology", "stack", "framework" },
            ["Process Optimization"] = new[] { "process", "workflow", "efficiency", "automation", "improvement" },
            ["Risk Management"] = new[] { "risk", "mitigation", "contingency", "issue", "blocker" }
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

    private double CalculateManagementUrgency(string request, DevOpsContext context)
    {
        var urgency = 0.5;

        if (request.ToLowerInvariant().Contains("urgent") || request.ToLowerInvariant().Contains("critical"))
            urgency += 0.3;

        if (request.ToLowerInvariant().Contains("deadline") || request.ToLowerInvariant().Contains("escalat"))
            urgency += 0.2;

        if (context.Project.Constraints.Any(c => c.Type == "Timeline" && c.Severity == "Critical"))
            urgency += 0.2;

        if (context.Team.HasCriticalIssues)
            urgency += 0.2;

        return Math.Min(1.0, urgency);
    }

    private TaskCategory DetermineTaskCategory(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("plan") || requestLower.Contains("strategy")) return TaskCategory.Planning;
        if (requestLower.Contains("architect") || requestLower.Contains("design")) return TaskCategory.Architecture;
        if (requestLower.Contains("document") || requestLower.Contains("report")) return TaskCategory.Documentation;
        if (requestLower.Contains("perform") || requestLower.Contains("metric")) return TaskCategory.Performance;
        if (requestLower.Contains("security") || requestLower.Contains("compliance")) return TaskCategory.Security;

        return TaskCategory.Planning;
    }

    private string AssessBusinessImpact(DevOpsContext context)
    {
        if (context.Project.Priority == "Critical" || context.Environment.IsProduction)
            return "High";
        if (context.Project.Priority == "High" || context.Team.TeamSize > 20)
            return "Medium";
        return "Low";
    }

    private Dictionary<string, object> ExtractManagementEntities(string request)
    {
        var entities = new Dictionary<string, object>();

        // Extract team-related entities
        var roles = new[] { "developer", "engineer", "architect", "analyst", "designer", "tester" };
        var mentionedRoles = roles.Where(role => request.ToLowerInvariant().Contains(role)).ToList();
        if (mentionedRoles.Any())
            entities["team_roles"] = mentionedRoles;

        // Extract methodology mentions
        var methodologies = new[] { "agile", "scrum", "kanban", "waterfall", "safe" };
        var mentionedMethods = methodologies.Where(method => request.ToLowerInvariant().Contains(method)).ToList();
        if (mentionedMethods.Any())
            entities["methodologies"] = mentionedMethods;

        // Extract timeline references
        if (request.ToLowerInvariant().Contains("quarter") || request.ToLowerInvariant().Contains("month"))
            entities["timeline_reference"] = true;

        return entities;
    }

    private async Task<PersonaResponse> GenerateTeamManagementResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateTeamGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Team Management",
            }
        };
        
        response.Metadata.Topics.Add("Team Leadership");
        response.Metadata.Topics.Add("Team Development");

        response.SuggestedActions.Add(new SuggestedAction
        {
            Title = "Schedule Team Meeting",
            Description = "Organize team discussion to address concerns",
            Priority = ActionPriority.High,
            Category = "Team Management"
        });

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateProjectPlanningResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateProjectPlanGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Project Planning",
            }
        };
        
        response.Metadata.Topics.Add("Project Management");
        response.Metadata.Topics.Add("Planning");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateResourceResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateResourceGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Resource Management",
            }
        };
        
        response.Metadata.Topics.Add("Resource Planning");
        response.Metadata.Topics.Add("Capacity Management");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GeneratePerformanceResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GeneratePerformanceGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Performance Management",
            }
        };
        
        response.Metadata.Topics.Add("Performance Management");
        response.Metadata.Topics.Add("Team Development");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateTechnicalDecisionResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateTechnicalDecisionGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Technical Decision",
            }
        };
        
        response.Metadata.Topics.Add("Technical Strategy");
        response.Metadata.Topics.Add("Decision Making");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateProcessResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateProcessGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Process Improvement",
            }
        };
        
        response.Metadata.Topics.Add("Process Optimization");
        response.Metadata.Topics.Add("Continuous Improvement");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateStakeholderResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateStakeholderGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Stakeholder Communication",
            }
        };
        
        response.Metadata.Topics.Add("Stakeholder Management");
        response.Metadata.Topics.Add("Communication");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateGeneralManagementResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = "I'm here to help with your management and leadership needs. Could you provide more details about the specific challenge you're facing?",
            Metadata = new ResponseMetadata
            {
                ResponseType = "General Management",
            }
        };
        
        // Topics already added in main method from analysis.Topics

        return await Task.FromResult(response);
    }

    private string GenerateTeamGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Based on your team management needs, here's my recommendation:");
        guidance.AppendLine();
        
        guidance.AppendLine($"**Current Team Context:**");
        guidance.AppendLine($"- Team Size: {context.Team.TeamSize} members");
        guidance.AppendLine($"- Maturity Level: {context.Team.MaturityLevel}");
        guidance.AppendLine($"- Current Phase: {context.Project.Stage}");
        guidance.AppendLine();
        
        guidance.AppendLine("**Team Development Strategy:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Immediate Actions**");
        guidance.AppendLine("   - Conduct 1:1s with each team member");
        guidance.AppendLine("   - Identify skills gaps and training needs");
        guidance.AppendLine("   - Address any immediate blockers or concerns");
        guidance.AppendLine("   - Review current workload distribution");
        guidance.AppendLine();
        guidance.AppendLine("2. **Team Building**");
        guidance.AppendLine("   - Establish clear team goals and OKRs");
        guidance.AppendLine("   - Create psychological safety for innovation");
        guidance.AppendLine("   - Implement regular team retrospectives");
        guidance.AppendLine("   - Foster cross-functional collaboration");
        guidance.AppendLine();
        guidance.AppendLine("3. **Performance Enhancement**");
        guidance.AppendLine("   - Define clear role expectations");
        guidance.AppendLine("   - Implement peer code reviews");
        guidance.AppendLine("   - Create mentorship programs");
        guidance.AppendLine("   - Recognize and reward achievements");
        
        if (context.Team.MaturityLevel == "Forming" || context.Team.MaturityLevel == "Storming")
        {
            guidance.AppendLine();
            guidance.AppendLine("**Team Formation Guidance:**");
            guidance.AppendLine("- Focus on establishing trust and communication");
            guidance.AppendLine("- Define team charter and working agreements");
            guidance.AppendLine("- Set up regular team ceremonies");
            guidance.AppendLine("- Address conflicts early and constructively");
        }
        
        return guidance.ToString();
    }

    private string GenerateProjectPlanGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Project planning recommendations for your initiative:");
        guidance.AppendLine();
        guidance.AppendLine("**Project Planning Framework:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Define Project Scope**");
        guidance.AppendLine("   - Clear objectives and success criteria");
        guidance.AppendLine("   - Stakeholder requirements and expectations");
        guidance.AppendLine("   - Technical and resource constraints");
        guidance.AppendLine("   - Risk assessment and mitigation plans");
        guidance.AppendLine();
        guidance.AppendLine("2. **Create Roadmap**");
        guidance.AppendLine("   - Break down into epics and stories");
        guidance.AppendLine("   - Define milestones and deliverables");
        guidance.AppendLine("   - Establish sprint/iteration cadence");
        guidance.AppendLine("   - Build in buffer for unknowns (20-30%)");
        guidance.AppendLine();
        guidance.AppendLine("3. **Resource Planning**");
        guidance.AppendLine("   - Skill matrix mapping");
        guidance.AppendLine("   - Capacity planning per sprint");
        guidance.AppendLine("   - External dependencies identification");
        guidance.AppendLine("   - Budget allocation and tracking");
        guidance.AppendLine();
        guidance.AppendLine("4. **Execution Strategy**");
        guidance.AppendLine("   - Daily standups for alignment");
        guidance.AppendLine("   - Weekly progress reviews");
        guidance.AppendLine("   - Monthly stakeholder updates");
        guidance.AppendLine("   - Continuous risk monitoring");
        
        if (context.Project.Methodology == "Agile")
        {
            guidance.AppendLine();
            guidance.AppendLine("**Agile-Specific Recommendations:**");
            guidance.AppendLine("- Use story points for estimation");
            guidance.AppendLine("- Maintain a groomed backlog (2-3 sprints ahead)");
            guidance.AppendLine("- Track velocity and burndown charts");
            guidance.AppendLine("- Regular sprint retrospectives for improvement");
        }
        
        return guidance.ToString();
    }

    private string GenerateResourceGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Resource allocation and capacity planning analysis:");
        guidance.AppendLine();
        guidance.AppendLine("**Current Resource Analysis:**");
        guidance.AppendLine($"- Team Size: {context.Team.TeamSize}");
        guidance.AppendLine($"- Current Utilization: {context.Team.CurrentUtilization}%");
        guidance.AppendLine($"- Project Priority: {context.Project.Priority}");
        guidance.AppendLine();
        guidance.AppendLine("**Resource Optimization Strategy:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Capacity Assessment**");
        guidance.AppendLine("   - Calculate available person-hours per sprint");
        guidance.AppendLine("   - Account for meetings and overhead (20-25%)");
        guidance.AppendLine("   - Consider vacation and training time");
        guidance.AppendLine("   - Identify skill bottlenecks");
        guidance.AppendLine();
        guidance.AppendLine("2. **Allocation Principles**");
        guidance.AppendLine("   - 70% on planned work");
        guidance.AppendLine("   - 20% on unplanned/support");
        guidance.AppendLine("   - 10% on innovation/learning");
        guidance.AppendLine();
        guidance.AppendLine("3. **Optimization Tactics**");
        guidance.AppendLine("   - Cross-training for flexibility");
        guidance.AppendLine("   - Pair programming for knowledge transfer");
        guidance.AppendLine("   - Automation to reduce manual effort");
        guidance.AppendLine("   - External resources for peak loads");
        
        if (context.Team.CurrentUtilization > 85)
        {
            guidance.AppendLine();
            guidance.AppendLine("**⚠️ High Utilization Warning:**");
            guidance.AppendLine("- Risk of burnout and quality issues");
            guidance.AppendLine("- Consider hiring or scope reduction");
            guidance.AppendLine("- Prioritize critical work only");
            guidance.AppendLine("- Implement work-in-progress limits");
        }
        
        return guidance.ToString();
    }

    private string GeneratePerformanceGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Performance management and team development guidance:");
        guidance.AppendLine();
        guidance.AppendLine("**Performance Framework:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Goal Setting (SMART)**");
        guidance.AppendLine("   - Specific: Clear, unambiguous objectives");
        guidance.AppendLine("   - Measurable: Quantifiable success metrics");
        guidance.AppendLine("   - Achievable: Realistic given constraints");
        guidance.AppendLine("   - Relevant: Aligned with team/company goals");
        guidance.AppendLine("   - Time-bound: Clear deadlines");
        guidance.AppendLine();
        guidance.AppendLine("2. **Continuous Feedback**");
        guidance.AppendLine("   - Weekly 1:1s (30 minutes minimum)");
        guidance.AppendLine("   - Real-time feedback on deliverables");
        guidance.AppendLine("   - Peer feedback collection");
        guidance.AppendLine("   - 360-degree reviews quarterly");
        guidance.AppendLine();
        guidance.AppendLine("3. **Performance Metrics**");
        guidance.AppendLine("   **Individual Metrics:**");
        guidance.AppendLine("   - Code quality (bugs, reviews)");
        guidance.AppendLine("   - Delivery velocity");
        guidance.AppendLine("   - Collaboration effectiveness");
        guidance.AppendLine("   - Learning & growth initiatives");
        guidance.AppendLine();
        guidance.AppendLine("   **Team Metrics:**");
        guidance.AppendLine("   - Sprint completion rate");
        guidance.AppendLine("   - Defect escape rate");
        guidance.AppendLine("   - Cycle time reduction");
        guidance.AppendLine("   - Customer satisfaction");
        guidance.AppendLine();
        guidance.AppendLine("4. **Development Planning**");
        guidance.AppendLine("   - Individual development plans (IDPs)");
        guidance.AppendLine("   - Skill gap analysis");
        guidance.AppendLine("   - Training budget allocation");
        guidance.AppendLine("   - Career pathway discussions");
        
        return guidance.ToString();
    }

    private string GenerateTechnicalDecisionGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Technical decision-making framework:");
        guidance.AppendLine();
        guidance.AppendLine("**Decision Process:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Requirements Analysis**");
        guidance.AppendLine("   - Functional requirements");
        guidance.AppendLine("   - Non-functional requirements (performance, security)");
        guidance.AppendLine("   - Scalability needs");
        guidance.AppendLine("   - Integration requirements");
        guidance.AppendLine();
        guidance.AppendLine("2. **Option Evaluation**");
        guidance.AppendLine("   - Technical feasibility");
        guidance.AppendLine("   - Team expertise and learning curve");
        guidance.AppendLine("   - Total cost of ownership");
        guidance.AppendLine("   - Vendor support and community");
        guidance.AppendLine("   - Future maintainability");
        guidance.AppendLine();
        guidance.AppendLine("3. **Risk Assessment**");
        guidance.AppendLine("   - Technical debt implications");
        guidance.AppendLine("   - Security vulnerabilities");
        guidance.AppendLine("   - Vendor lock-in risks");
        guidance.AppendLine("   - Scalability limitations");
        guidance.AppendLine();
        guidance.AppendLine("4. **Decision Documentation**");
        guidance.AppendLine("   - Architecture Decision Record (ADR)");
        guidance.AppendLine("   - Trade-off analysis");
        guidance.AppendLine("   - Implementation roadmap");
        guidance.AppendLine("   - Success criteria");
        
        if (Configuration.DecisionMakingStyle == DecisionMakingStyle.Consensus)
        {
            guidance.AppendLine();
            guidance.AppendLine("**Consensus Building:**");
            guidance.AppendLine("- Technical review with architects");
            guidance.AppendLine("- Team feedback sessions");
            guidance.AppendLine("- Proof of concept development");
            guidance.AppendLine("- Stakeholder alignment meetings");
        }
        
        return guidance.ToString();
    }

    private string GenerateProcessGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Process improvement recommendations:");
        guidance.AppendLine();
        guidance.AppendLine("**Current Process Assessment:**");
        guidance.AppendLine($"- Methodology: {context.Project.Methodology}");
        guidance.AppendLine($"- Team Maturity: {context.Team.MaturityLevel}");
        guidance.AppendLine($"- Automation Level: {context.Team.AutomationLevel}%");
        guidance.AppendLine();
        guidance.AppendLine("**Improvement Strategy:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Quick Wins (0-30 days)**");
        guidance.AppendLine("   - Automate repetitive manual tasks");
        guidance.AppendLine("   - Standardize code review process");
        guidance.AppendLine("   - Implement daily standups");
        guidance.AppendLine("   - Create team dashboards");
        guidance.AppendLine();
        guidance.AppendLine("2. **Medium Term (1-3 months)**");
        guidance.AppendLine("   - CI/CD pipeline optimization");
        guidance.AppendLine("   - Test automation framework");
        guidance.AppendLine("   - Documentation standards");
        guidance.AppendLine("   - Metrics and KPI tracking");
        guidance.AppendLine();
        guidance.AppendLine("3. **Long Term (3-6 months)**");
        guidance.AppendLine("   - DevOps culture transformation");
        guidance.AppendLine("   - Full automation strategy");
        guidance.AppendLine("   - Continuous improvement culture");
        guidance.AppendLine("   - Innovation time allocation");
        guidance.AppendLine();
        guidance.AppendLine("**Success Metrics:**");
        guidance.AppendLine("- Cycle time reduction: Target 30%");
        guidance.AppendLine("- Defect rate reduction: Target 50%");
        guidance.AppendLine("- Deployment frequency: 2x increase");
        guidance.AppendLine("- Team satisfaction: >8/10");
        
        return guidance.ToString();
    }

    private string GenerateStakeholderGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        var businessImpact = AssessBusinessImpact(context);
        
        guidance.AppendLine("Stakeholder communication strategy:");
        guidance.AppendLine();
        guidance.AppendLine($"**Context:** Business Impact Level: {businessImpact}");
        guidance.AppendLine();
        guidance.AppendLine("**Communication Framework:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Executive Updates**");
        guidance.AppendLine("   - Monthly status reports");
        guidance.AppendLine("   - KPI dashboards");
        guidance.AppendLine("   - Risk and mitigation summaries");
        guidance.AppendLine("   - Budget vs. actual tracking");
        guidance.AppendLine();
        guidance.AppendLine("2. **Key Messages**");
        guidance.AppendLine("   - Progress against milestones");
        guidance.AppendLine("   - Value delivered to date");
        guidance.AppendLine("   - Upcoming deliverables");
        guidance.AppendLine("   - Resource needs/blockers");
        guidance.AppendLine();
        guidance.AppendLine("3. **Communication Channels**");
        guidance.AppendLine("   - Executive briefings (monthly)");
        guidance.AppendLine("   - Steering committee meetings");
        guidance.AppendLine("   - Status dashboards (real-time)");
        guidance.AppendLine("   - Escalation procedures");
        guidance.AppendLine();
        guidance.AppendLine("4. **Stakeholder Matrix**");
        guidance.AppendLine("   **High Power, High Interest:**");
        guidance.AppendLine("   - Manage closely");
        guidance.AppendLine("   - Regular 1:1 updates");
        guidance.AppendLine("   - Involve in key decisions");
        guidance.AppendLine();
        guidance.AppendLine("   **High Power, Low Interest:**");
        guidance.AppendLine("   - Keep satisfied");
        guidance.AppendLine("   - Executive summaries only");
        guidance.AppendLine("   - Escalate critical issues");
        
        if (businessImpact == "High")
        {
            guidance.AppendLine();
            guidance.AppendLine("**High Impact Considerations:**");
            guidance.AppendLine("- Weekly executive updates recommended");
            guidance.AppendLine("- Prepare contingency communications");
            guidance.AppendLine("- Establish war room if needed");
            guidance.AppendLine("- Media/PR coordination plans");
        }
        
        return guidance.ToString();
    }

    private List<SuggestedAction> GenerateManagementActions(RequestAnalysis analysis, DevOpsContext context)
    {
        var actions = new List<SuggestedAction>();

        if (analysis.Intent == "Team Management" && context.Team.HasCriticalIssues)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Address Team Issues",
                Description = "Schedule immediate team health check",
                Priority = ActionPriority.Critical,
                Category = "Team Management"
            });
        }

        if (context.Project.Stage == "Planning")
        {
            actions.Add(new SuggestedAction
            {
                Title = "Finalize Project Plan",
                Description = "Complete resource allocation and timeline",
                Priority = ActionPriority.High,
                Category = "Planning"
            });
        }

        if (context.Team.CurrentUtilization > 90)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Resource Rebalancing",
                Description = "Review and redistribute workload",
                Priority = ActionPriority.Critical,
                Category = "Resource Management"
            });
        }

        if (analysis.Topics.Contains("Risk Management"))
        {
            actions.Add(new SuggestedAction
            {
                Title = "Risk Assessment",
                Description = "Conduct comprehensive risk review",
                Priority = ActionPriority.High,
                Category = "Risk Management"
            });
        }

        return actions;
    }

    private PersonaConfidence CalculateResponseConfidence(RequestAnalysis analysis, DevOpsContext context)
    {
        var confidence = new PersonaConfidence
        {
            DomainExpertise = 0.85,
            ContextRelevance = CalculateContextRelevance(analysis, context),
            ResponseQuality = 0.85
        };

        confidence.Overall = (confidence.DomainExpertise * 0.4 + 
                            confidence.ContextRelevance * 0.3 + 
                            confidence.ResponseQuality * 0.3);

        // Add management-specific caveats
        if (context.Team.TeamSize > 50)
        {
            confidence.Caveats.Add("Large team size may require additional organizational considerations");
        }

        if (context.Project.Constraints.Any(c => c.Type == "Budget" && c.Severity == "Critical"))
        {
            confidence.Caveats.Add("Budget constraints may limit implementation options");
        }

        if (analysis.Intent == "Stakeholder Communication" && businessImpact == "High")
        {
            confidence.Caveats.Add("High-impact communications should be reviewed by leadership");
        }

        return confidence;
    }

    private double CalculateContextRelevance(RequestAnalysis analysis, DevOpsContext context)
    {
        var relevance = 0.6;

        // Management is relevant across all contexts
        if (context.Team.TeamSize > 0)
            relevance += 0.1;

        if (context.Project.Stage == "Planning" || context.Project.Stage == "Execution")
            relevance += 0.1;

        if (analysis.Topics.Any(topic => GetPersonaSkills().Contains(topic, StringComparer.OrdinalIgnoreCase)))
            relevance += 0.2;

        return Math.Min(1.0, relevance);
    }

    private string businessImpact => "Medium"; // Default for when not in context
}