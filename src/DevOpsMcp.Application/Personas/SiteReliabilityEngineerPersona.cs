using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Logging;
using MediatR;

namespace DevOpsMcp.Application.Personas;

public class SiteReliabilityEngineerPersona : BaseDevOpsPersona
{
    private readonly IMediator _mediator;
    private const string PersonaId = "sre-specialist";
    
    public SiteReliabilityEngineerPersona(
        ILogger<SiteReliabilityEngineerPersona> logger,
        IPersonaMemoryManager memoryManager,
        IMediator mediator) 
        : base(logger, memoryManager)
    {
        _mediator = mediator;
        Initialize();
    }

    public override string Id => PersonaId;
    public override string Name => "Site Reliability Engineer";
    public override string Role => "Reliability and Performance Specialist";
    public override string Description => "Expert in system reliability, monitoring, incident response, performance optimization, and SLO management";
    public override DevOpsSpecialization Specialization => DevOpsSpecialization.Reliability;

    protected override PersonaConfiguration GetDefaultConfiguration()
    {
        return new PersonaConfiguration
        {
            CommunicationStyle = CommunicationStyle.TechnicalPrecise,
            TechnicalDepth = TechnicalDepth.Expert,
            RiskTolerance = RiskTolerance.Conservative,
            DecisionMakingStyle = DecisionMakingStyle.DataDriven,
            CollaborationPreferences = CollaborationPreferences.CrossFunctional,
            SecurityPosture = SecurityPosture.Strict
        };
    }

    protected override Dictionary<string, object> InitializeCapabilities()
    {
        return new Dictionary<string, object>
        {
            ["incident_response"] = new List<string> { "Incident Management", "Root Cause Analysis", "Post-Mortems", "Runbooks" },
            ["monitoring_observability"] = new List<string> { "Prometheus", "Grafana", "Datadog", "New Relic", "Azure Monitor" },
            ["performance_optimization"] = new List<string> { "Load Testing", "Capacity Planning", "Performance Tuning", "Resource Optimization" },
            ["reliability_engineering"] = new List<string> { "SLI/SLO/SLA", "Error Budgets", "Chaos Engineering", "Disaster Recovery" },
            ["automation"] = new List<string> { "Automated Remediation", "Self-Healing Systems", "Runbook Automation", "Alert Automation" },
            ["scalability"] = new List<string> { "Auto-scaling", "Load Balancing", "Distributed Systems", "High Availability" },
            ["tools"] = new List<string> { "Kubernetes", "Terraform", "Ansible", "Python", "Go" },
            ["practices"] = new List<string> { "Blameless Post-Mortems", "Toil Reduction", "On-Call Rotation", "Game Days" }
        };
    }

    protected override async Task<RequestAnalysis> AnalyzeRequestAsync(string request, DevOpsContext context)
    {
        var analysis = new RequestAnalysis
        {
            Intent = DetermineSREIntent(request),
            Urgency = CalculateSREUrgency(request, context),
            EstimatedCategory = EstimateSRETaskCategory(request)
        };

        // Add topics
        foreach (var topic in ExtractSRETopics(request))
        {
            analysis.Topics.Add(topic);
        }
        
        // Add context
        analysis.Context["environment"] = context.Environment.EnvironmentType;
        analysis.Context["is_production"] = context.Environment.IsProduction.ToString();
        analysis.Context["current_load"] = context.Performance.MaxConcurrentRequests.ToString();
        analysis.Context["risk_level"] = CalculateRiskLevel(context);

        // Extract entities
        var entities = ExtractSREEntities(request);
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
                ResponseType = "SRE Analysis",
            }
        };

        switch (analysis.Intent)
        {
            case "Incident Response":
                response = await GenerateIncidentResponse(analysis, context);
                break;
            case "Performance Analysis":
                response = await GeneratePerformanceResponse(analysis, context);
                break;
            case "Monitoring Setup":
                response = await GenerateMonitoringResponse(analysis, context);
                break;
            case "Reliability Assessment":
                response = await GenerateReliabilityResponse(analysis, context);
                break;
            case "Capacity Planning":
                response = await GenerateCapacityResponse(analysis, context);
                break;
            case "Post-Mortem":
                response = await GeneratePostMortemResponse(analysis, context);
                break;
            default:
                response = await GenerateGeneralSREResponse(analysis, context);
                break;
        }

        // Add topics to response
        foreach (var topic in analysis.Topics)
        {
            response.Metadata.Topics.Add(topic);
        }
        
        // Add suggested actions
        foreach (var action in GenerateSREActions(analysis, context))
        {
            response.SuggestedActions.Add(action);
        }
        
        response.Confidence = CalculateSREConfidence(analysis, context);

        return response;
    }

    protected override Dictionary<string, double> GetAlignmentWeights()
    {
        return new Dictionary<string, double>
        {
            ["category"] = 0.25,
            ["skills"] = 0.35,
            ["complexity"] = 0.25,
            ["specialization"] = 0.15
        };
    }

    protected override Dictionary<TaskCategory, double> GetCategoryAlignmentMap()
    {
        return new Dictionary<TaskCategory, double>
        {
            [TaskCategory.Monitoring] = 1.0,
            [TaskCategory.Performance] = 1.0,
            [TaskCategory.Troubleshooting] = 0.9,
            [TaskCategory.Infrastructure] = 0.7,
            [TaskCategory.Security] = 0.6,
            [TaskCategory.Automation] = 0.8,
            [TaskCategory.Architecture] = 0.7,
            [TaskCategory.Deployment] = 0.5,
            [TaskCategory.Planning] = 0.6,
            [TaskCategory.Documentation] = 0.5
        };
    }

    protected override List<string> GetPersonaSkills()
    {
        return new List<string>
        {
            "Monitoring", "Observability", "Incident Response", "Performance Tuning",
            "Capacity Planning", "SLI/SLO", "Reliability Engineering", "Chaos Engineering",
            "Root Cause Analysis", "Kubernetes", "Distributed Systems", "Load Balancing",
            "Auto-scaling", "Disaster Recovery", "High Availability", "Metrics Analysis",
            "Alert Management", "Runbook Creation", "Post-Mortems", "Python", "Go"
        };
    }

    private string DetermineSREIntent(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("incident") || requestLower.Contains("outage") || requestLower.Contains("down"))
            return "Incident Response";
        if (requestLower.Contains("performance") || requestLower.Contains("slow") || requestLower.Contains("latency"))
            return "Performance Analysis";
        if (requestLower.Contains("monitor") || requestLower.Contains("alert") || requestLower.Contains("observability"))
            return "Monitoring Setup";
        if (requestLower.Contains("reliability") || requestLower.Contains("slo") || requestLower.Contains("availability"))
            return "Reliability Assessment";
        if (requestLower.Contains("capacity") || requestLower.Contains("scale") || requestLower.Contains("growth"))
            return "Capacity Planning";
        if (requestLower.Contains("post-mortem") || requestLower.Contains("rca") || requestLower.Contains("root cause"))
            return "Post-Mortem";

        return "General SRE";
    }

    private List<string> ExtractSRETopics(string request)
    {
        var topics = new List<string>();
        var requestLower = request.ToLowerInvariant();

        var topicKeywords = new Dictionary<string, string[]>
        {
            ["Incident Management"] = new[] { "incident", "outage", "emergency", "critical" },
            ["Performance"] = new[] { "performance", "latency", "response time", "throughput" },
            ["Monitoring"] = new[] { "monitor", "metrics", "logs", "traces", "observability" },
            ["Reliability"] = new[] { "reliability", "availability", "uptime", "slo", "sla" },
            ["Scalability"] = new[] { "scale", "capacity", "load", "traffic" },
            ["Automation"] = new[] { "automate", "self-healing", "remediation" }
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

    private double CalculateSREUrgency(string request, DevOpsContext context)
    {
        var urgency = 0.3; // Base urgency

        var requestLower = request.ToLowerInvariant();

        // Critical keywords
        if (requestLower.Contains("down") || requestLower.Contains("outage"))
            urgency = 0.9;
        else if (requestLower.Contains("critical") || requestLower.Contains("emergency"))
            urgency = 0.8;
        else if (requestLower.Contains("degraded") || requestLower.Contains("slow"))
            urgency = 0.6;

        // Production environment modifier
        if (context.Environment.IsProduction)
            urgency = Math.Min(1.0, urgency + 0.2);

        // High load modifier
        if (context.Performance.MaxConcurrentRequests > 500)
            urgency = Math.Min(1.0, urgency + 0.1);

        return urgency;
    }

    private TaskCategory EstimateSRETaskCategory(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("monitor") || requestLower.Contains("observ"))
            return TaskCategory.Monitoring;
        if (requestLower.Contains("performance") || requestLower.Contains("optimiz"))
            return TaskCategory.Performance;
        if (requestLower.Contains("incident") || requestLower.Contains("troubleshoot"))
            return TaskCategory.Troubleshooting;
        if (requestLower.Contains("scale") || requestLower.Contains("capacity"))
            return TaskCategory.Infrastructure;

        return TaskCategory.Monitoring;
    }

    private string CalculateRiskLevel(DevOpsContext context)
    {
        if (context.Environment.IsProduction && context.Environment.IsRegulated)
            return "Critical";
        if (context.Environment.IsProduction)
            return "High";
        if (context.Environment.EnvironmentType == "Staging")
            return "Medium";
        
        return "Low";
    }

    private Dictionary<string, object> ExtractSREEntities(string request)
    {
        var entities = new Dictionary<string, object>();

        // Extract metrics
        var metrics = new List<string>();
        var metricKeywords = new[] { "cpu", "memory", "disk", "network", "latency", "throughput", "error rate" };
        foreach (var metric in metricKeywords)
        {
            if (request.ToLowerInvariant().Contains(metric))
                metrics.Add(metric);
        }
        if (metrics.Any())
            entities["metrics"] = metrics;

        // Extract time ranges
        var timePatterns = new[] { "last hour", "past 24 hours", "last week", "today", "yesterday" };
        var timeRanges = timePatterns.Where(t => request.ToLowerInvariant().Contains(t)).ToList();
        if (timeRanges.Any())
            entities["time_ranges"] = timeRanges;

        // Extract severity levels
        var severities = new[] { "critical", "high", "medium", "low", "warning" };
        var mentionedSeverities = severities.Where(s => request.ToLowerInvariant().Contains(s)).ToList();
        if (mentionedSeverities.Any())
            entities["severities"] = mentionedSeverities;

        return entities;
    }

    private async Task<PersonaResponse> GenerateIncidentResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateIncidentResponsePlan(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Incident Response",
                Tone = "Urgent"
            }
        };

        response.Metadata.Topics.Add("Incident Management");
        response.Metadata.Topics.Add("Emergency Response");
        
        // Add immediate actions
        response.SuggestedActions.Add(new SuggestedAction
        {
            Title = "Initiate Incident Response",
            Description = "Start incident response protocol immediately",
            Priority = ActionPriority.Critical,
            Category = "Incident",
            Impact = new EstimatedImpact
            {
                TimeToComplete = "Immediate",
                EffortLevel = "High"
            }
        });

        response.Context["incident_severity"] = DetermineIncidentSeverity(analysis, context);
        response.Context["escalation_required"] = (analysis.Urgency > 0.7).ToString();

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GeneratePerformanceResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GeneratePerformanceAnalysis(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Performance Analysis",
            }
        };

        response.Metadata.Topics.Add("Performance");
        response.Metadata.Topics.Add("Optimization");
        
        // Add performance metrics to context
        var performanceMetrics = AnalyzePerformanceMetrics(context);
        response.Context["performance_metrics"] = performanceMetrics;

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateMonitoringResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateMonitoringStrategy(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Monitoring Configuration",
            }
        };
        
        response.Metadata.Topics.Add("Monitoring");
        response.Metadata.Topics.Add("Observability");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateReliabilityResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateReliabilityAssessment(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Reliability Assessment",
            }
        };

        response.Metadata.Topics.Add("Reliability");
        response.Metadata.Topics.Add("SLO Management");
        
        // Calculate current reliability metrics
        var reliabilityScore = CalculateReliabilityScore(context);
        response.Context["reliability_score"] = reliabilityScore;
        response.Context["meets_slo"] = (reliabilityScore > 0.99).ToString();

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateCapacityResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateCapacityPlan(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Capacity Planning",
            }
        };
        
        response.Metadata.Topics.Add("Scalability");
        response.Metadata.Topics.Add("Capacity Management");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GeneratePostMortemResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GeneratePostMortemTemplate(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Post-Mortem Analysis",
            }
        };
        
        response.Metadata.Topics.Add("Post-Mortem");
        response.Metadata.Topics.Add("Root Cause Analysis");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateGeneralSREResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = "As an SRE, I'm here to help ensure your systems are reliable, performant, and observable. What specific reliability concerns can I help you address?",
            Metadata = new ResponseMetadata
            {
                ResponseType = "General SRE Guidance",
            }
        };
        
        // Topics already added in main method from analysis.Topics

        return await Task.FromResult(response);
    }

    private string GenerateIncidentResponsePlan(RequestAnalysis analysis, DevOpsContext context)
    {
        var plan = new System.Text.StringBuilder();
        var severity = DetermineIncidentSeverity(analysis, context);
        
        plan.AppendLine($"🚨 **INCIDENT RESPONSE - SEVERITY: {severity}**");
        plan.AppendLine();
        plan.AppendLine("**IMMEDIATE ACTIONS:**");
        plan.AppendLine();
        plan.AppendLine("1. **ASSESS** (0-5 minutes)");
        plan.AppendLine("   - Verify the issue and impact scope");
        plan.AppendLine("   - Check monitoring dashboards and alerts");
        plan.AppendLine("   - Identify affected services and users");
        plan.AppendLine();
        plan.AppendLine("2. **COMMUNICATE** (5-10 minutes)");
        plan.AppendLine("   - Create incident channel/bridge");
        plan.AppendLine("   - Notify on-call team members");
        plan.AppendLine("   - Update status page if customer-facing");
        plan.AppendLine();
        plan.AppendLine("3. **CONTAIN** (10-30 minutes)");
        plan.AppendLine("   - Implement immediate mitigation");
        plan.AppendLine("   - Consider rollback if recent deployment");
        plan.AppendLine("   - Scale resources if capacity issue");
        plan.AppendLine();
        plan.AppendLine("4. **DIAGNOSE** (Ongoing)");
        plan.AppendLine("   - Collect logs and metrics");
        plan.AppendLine("   - Run diagnostic commands");
        plan.AppendLine("   - Document timeline of events");
        
        if (severity == "Critical" || context.Environment.IsProduction)
        {
            plan.AppendLine();
            plan.AppendLine("**ESCALATION REQUIRED:**");
            plan.AppendLine("- Page senior SRE immediately");
            plan.AppendLine("- Notify engineering leadership");
            plan.AppendLine("- Prepare executive communication");
        }
        
        plan.AppendLine();
        plan.AppendLine("**Key Metrics to Monitor:**");
        plan.AppendLine("- Error rates");
        plan.AppendLine("- Response times");
        plan.AppendLine("- Resource utilization");
        plan.AppendLine("- User impact metrics");
        
        return plan.ToString();
    }

    private string GeneratePerformanceAnalysis(RequestAnalysis analysis, DevOpsContext context)
    {
        var analysis_text = new System.Text.StringBuilder();
        
        analysis_text.AppendLine("## Performance Analysis Report");
        analysis_text.AppendLine();
        analysis_text.AppendLine("### Current Performance Metrics");
        analysis_text.AppendLine($"- **Response Time Target**: {context.Performance.MaxResponseTimeMs}ms");
        analysis_text.AppendLine($"- **CPU Threshold**: {context.Performance.MaxCpuPercent}%");
        analysis_text.AppendLine($"- **Memory Limit**: {context.Performance.MaxMemoryMb}MB");
        analysis_text.AppendLine($"- **Concurrent Requests**: {context.Performance.MaxConcurrentRequests}");
        analysis_text.AppendLine();
        
        analysis_text.AppendLine("### Performance Investigation Steps");
        analysis_text.AppendLine();
        analysis_text.AppendLine("1. **Baseline Analysis**");
        analysis_text.AppendLine("   ```bash");
        analysis_text.AppendLine("   # Check current resource usage");
        analysis_text.AppendLine("   kubectl top nodes");
        analysis_text.AppendLine("   kubectl top pods -n production");
        analysis_text.AppendLine("   ```");
        analysis_text.AppendLine();
        analysis_text.AppendLine("2. **Application Profiling**");
        analysis_text.AppendLine("   - Enable application performance monitoring");
        analysis_text.AppendLine("   - Identify slow queries/operations");
        analysis_text.AppendLine("   - Check for memory leaks");
        analysis_text.AppendLine();
        analysis_text.AppendLine("3. **Infrastructure Analysis**");
        analysis_text.AppendLine("   - Review load balancer metrics");
        analysis_text.AppendLine("   - Check database performance");
        analysis_text.AppendLine("   - Analyze network latency");
        analysis_text.AppendLine();
        analysis_text.AppendLine("4. **Optimization Recommendations**");
        
        if (context.Performance.MaxConcurrentRequests > 500)
        {
            analysis_text.AppendLine("   - Consider implementing caching layer");
            analysis_text.AppendLine("   - Evaluate horizontal scaling options");
            analysis_text.AppendLine("   - Review database connection pooling");
        }
        
        if (Configuration.TechnicalDepth == TechnicalDepth.Expert)
        {
            analysis_text.AppendLine();
            analysis_text.AppendLine("### Advanced Performance Tuning");
            analysis_text.AppendLine("- Implement request coalescing");
            analysis_text.AppendLine("- Consider edge caching with CDN");
            analysis_text.AppendLine("- Optimize container resource limits");
            analysis_text.AppendLine("- Review garbage collection settings");
        }
        
        return analysis_text.ToString();
    }

    private string GenerateMonitoringStrategy(RequestAnalysis analysis, DevOpsContext context)
    {
        var strategy = new System.Text.StringBuilder();
        
        strategy.AppendLine("## Comprehensive Monitoring Strategy");
        strategy.AppendLine();
        strategy.AppendLine("### Four Golden Signals");
        strategy.AppendLine("1. **Latency** - Time to service requests");
        strategy.AppendLine("2. **Traffic** - Demand on your system");
        strategy.AppendLine("3. **Errors** - Rate of failed requests");
        strategy.AppendLine("4. **Saturation** - System resource usage");
        strategy.AppendLine();
        
        strategy.AppendLine("### Monitoring Stack Recommendations");
        strategy.AppendLine();
        
        if (context.TechStack.CloudProvider == "Azure")
        {
            strategy.AppendLine("**Azure-Native Monitoring:**");
            strategy.AppendLine("- Azure Monitor for infrastructure metrics");
            strategy.AppendLine("- Application Insights for APM");
            strategy.AppendLine("- Log Analytics for centralized logging");
            strategy.AppendLine("- Azure Alerts for notification");
        }
        
        strategy.AppendLine();
        strategy.AppendLine("**Open Source Stack:**");
        strategy.AppendLine("- **Metrics**: Prometheus + Grafana");
        strategy.AppendLine("- **Logs**: ELK Stack or Loki");
        strategy.AppendLine("- **Traces**: Jaeger or Zipkin");
        strategy.AppendLine("- **Alerts**: AlertManager");
        strategy.AppendLine();
        
        strategy.AppendLine("### Key Metrics to Monitor");
        strategy.AppendLine();
        strategy.AppendLine("**Application Level:**");
        strategy.AppendLine("- Request rate (req/sec)");
        strategy.AppendLine("- Error rate (4xx, 5xx)");
        strategy.AppendLine("- Response time (p50, p95, p99)");
        strategy.AppendLine("- Active connections");
        strategy.AppendLine();
        strategy.AppendLine("**Infrastructure Level:**");
        strategy.AppendLine("- CPU utilization");
        strategy.AppendLine("- Memory usage");
        strategy.AppendLine("- Disk I/O");
        strategy.AppendLine("- Network throughput");
        strategy.AppendLine();
        
        if (context.Environment.IsProduction)
        {
            strategy.AppendLine("### Production-Specific Monitoring");
            strategy.AppendLine("- Real User Monitoring (RUM)");
            strategy.AppendLine("- Synthetic monitoring for critical paths");
            strategy.AppendLine("- Business KPI dashboards");
            strategy.AppendLine("- SLO tracking dashboards");
        }
        
        strategy.AppendLine();
        strategy.AppendLine("### Alert Configuration");
        strategy.AppendLine("```yaml");
        strategy.AppendLine("# Example Prometheus alert");
        strategy.AppendLine("- alert: HighErrorRate");
        strategy.AppendLine("  expr: rate(http_requests_total{status=~\"5..\"}[5m]) > 0.05");
        strategy.AppendLine("  for: 5m");
        strategy.AppendLine("  labels:");
        strategy.AppendLine("    severity: critical");
        strategy.AppendLine("  annotations:");
        strategy.AppendLine("    summary: High error rate detected");
        strategy.AppendLine("```");
        
        return strategy.ToString();
    }

    private string GenerateReliabilityAssessment(RequestAnalysis analysis, DevOpsContext context)
    {
        var assessment = new System.Text.StringBuilder();
        var reliabilityScore = CalculateReliabilityScore(context);
        
        assessment.AppendLine("## System Reliability Assessment");
        assessment.AppendLine();
        assessment.AppendLine($"**Current Reliability Score**: {reliabilityScore:P2}");
        assessment.AppendLine($"**Target SLO**: 99.9% (43.2 minutes downtime/month)");
        assessment.AppendLine();
        
        assessment.AppendLine("### Service Level Indicators (SLIs)");
        assessment.AppendLine("1. **Availability SLI**");
        assessment.AppendLine("   - Measurement: Successful requests / Total requests");
        assessment.AppendLine("   - Current: Calculate from last 30 days");
        assessment.AppendLine();
        assessment.AppendLine("2. **Latency SLI**");
        assessment.AppendLine($"   - Target: 95% of requests < {context.Performance.MaxResponseTimeMs}ms");
        assessment.AppendLine("   - Measurement: Response time at p95");
        assessment.AppendLine();
        assessment.AppendLine("3. **Quality SLI**");
        assessment.AppendLine("   - Error rate < 0.1%");
        assessment.AppendLine("   - Data consistency checks passing");
        assessment.AppendLine();
        
        assessment.AppendLine("### Reliability Improvements");
        assessment.AppendLine();
        
        if (reliabilityScore < 0.999)
        {
            assessment.AppendLine("**🔴 Critical Improvements Needed:**");
            assessment.AppendLine("- Implement redundancy for single points of failure");
            assessment.AppendLine("- Add health checks and auto-healing");
            assessment.AppendLine("- Review and test disaster recovery procedures");
        }
        else
        {
            assessment.AppendLine("**✅ System meets reliability targets**");
            assessment.AppendLine();
            assessment.AppendLine("**Recommended Enhancements:**");
            assessment.AppendLine("- Implement chaos engineering practices");
            assessment.AppendLine("- Enhance observability coverage");
            assessment.AppendLine("- Automate more runbook procedures");
        }
        
        assessment.AppendLine();
        assessment.AppendLine("### Error Budget Status");
        var errorBudget = CalculateErrorBudget(reliabilityScore);
        assessment.AppendLine($"- Monthly error budget: {errorBudget} minutes");
        assessment.AppendLine($"- Consumed this month: TBD (requires historical data)");
        assessment.AppendLine($"- Remaining: TBD");
        
        if (context.Environment.IsProduction)
        {
            assessment.AppendLine();
            assessment.AppendLine("### Production Readiness Checklist");
            assessment.AppendLine("- [ ] Automated rollback capability");
            assessment.AppendLine("- [ ] Comprehensive monitoring");
            assessment.AppendLine("- [ ] Documented runbooks");
            assessment.AppendLine("- [ ] Load tested to 2x expected traffic");
            assessment.AppendLine("- [ ] Disaster recovery tested");
            assessment.AppendLine("- [ ] Security scanning passed");
        }
        
        return assessment.ToString();
    }

    private string GenerateCapacityPlan(RequestAnalysis analysis, DevOpsContext context)
    {
        var plan = new System.Text.StringBuilder();
        
        plan.AppendLine("## Capacity Planning Analysis");
        plan.AppendLine();
        plan.AppendLine("### Current Capacity Metrics");
        plan.AppendLine($"- **Max Concurrent Requests**: {context.Performance.MaxConcurrentRequests}");
        plan.AppendLine($"- **CPU Limit**: {context.Performance.MaxCpuPercent}%");
        plan.AppendLine($"- **Memory Limit**: {context.Performance.MaxMemoryMb}MB");
        plan.AppendLine();
        
        plan.AppendLine("### Capacity Planning Process");
        plan.AppendLine();
        plan.AppendLine("1. **Baseline Measurement**");
        plan.AppendLine("   - Current peak usage patterns");
        plan.AppendLine("   - Resource utilization trends");
        plan.AppendLine("   - Growth rate analysis");
        plan.AppendLine();
        plan.AppendLine("2. **Load Testing**");
        plan.AppendLine("   ```bash");
        plan.AppendLine("   # Example load test with k6");
        plan.AppendLine("   k6 run --vus 100 --duration 30m load-test.js");
        plan.AppendLine("   ```");
        plan.AppendLine();
        plan.AppendLine("3. **Scaling Recommendations**");
        
        var scalingFactor = CalculateScalingNeeds(context);
        if (scalingFactor > 1.5)
        {
            plan.AppendLine("   **⚠️ Scaling Required Soon**");
            plan.AppendLine($"   - Projected growth: {scalingFactor:P0}");
            plan.AppendLine("   - Recommend horizontal scaling");
            plan.AppendLine("   - Consider auto-scaling policies");
        }
        else
        {
            plan.AppendLine("   **✅ Current capacity adequate**");
            plan.AppendLine("   - Monitor growth trends");
            plan.AppendLine("   - Plan for 20% headroom");
        }
        
        plan.AppendLine();
        plan.AppendLine("### Auto-Scaling Configuration");
        plan.AppendLine("```yaml");
        plan.AppendLine("# Kubernetes HPA example");
        plan.AppendLine("apiVersion: autoscaling/v2");
        plan.AppendLine("kind: HorizontalPodAutoscaler");
        plan.AppendLine("metadata:");
        plan.AppendLine("  name: app-hpa");
        plan.AppendLine("spec:");
        plan.AppendLine("  minReplicas: 3");
        plan.AppendLine("  maxReplicas: 10");
        plan.AppendLine("  targetCPUUtilizationPercentage: 70");
        plan.AppendLine("  targetMemoryUtilizationPercentage: 80");
        plan.AppendLine("```");
        
        return plan.ToString();
    }

    private string GeneratePostMortemTemplate(RequestAnalysis analysis, DevOpsContext context)
    {
        var template = new System.Text.StringBuilder();
        
        template.AppendLine("# Post-Mortem Report Template");
        template.AppendLine();
        template.AppendLine("## Incident Summary");
        template.AppendLine("- **Date**: [YYYY-MM-DD]");
        template.AppendLine("- **Duration**: [XX minutes/hours]");
        template.AppendLine("- **Impact**: [Users affected, services impacted]");
        template.AppendLine("- **Severity**: [P1/P2/P3]");
        template.AppendLine();
        template.AppendLine("## Timeline");
        template.AppendLine("- **HH:MM** - Initial detection");
        template.AppendLine("- **HH:MM** - Incident declared");
        template.AppendLine("- **HH:MM** - Root cause identified");
        template.AppendLine("- **HH:MM** - Mitigation applied");
        template.AppendLine("- **HH:MM** - Service restored");
        template.AppendLine();
        template.AppendLine("## Root Cause Analysis");
        template.AppendLine();
        template.AppendLine("### What Happened?");
        template.AppendLine("[Detailed description of the incident]");
        template.AppendLine();
        template.AppendLine("### Why Did It Happen?");
        template.AppendLine("[Root cause analysis - use 5 Whys technique]");
        template.AppendLine();
        template.AppendLine("### Contributing Factors");
        template.AppendLine("- [ ] Recent deployments");
        template.AppendLine("- [ ] Configuration changes");
        template.AppendLine("- [ ] Infrastructure issues");
        template.AppendLine("- [ ] External dependencies");
        template.AppendLine();
        template.AppendLine("## Impact Analysis");
        template.AppendLine("- **Customer Impact**: [Quantify user impact]");
        template.AppendLine("- **Business Impact**: [Revenue, SLA breaches]");
        template.AppendLine("- **Technical Debt**: [What shortcuts were taken]");
        template.AppendLine();
        template.AppendLine("## What Went Well");
        template.AppendLine("- [Quick detection due to monitoring]");
        template.AppendLine("- [Effective team communication]");
        template.AppendLine("- [Runbook was helpful]");
        template.AppendLine();
        template.AppendLine("## What Could Be Improved");
        template.AppendLine("- [Alert could have fired sooner]");
        template.AppendLine("- [Rollback process was slow]");
        template.AppendLine("- [Documentation was outdated]");
        template.AppendLine();
        template.AppendLine("## Action Items");
        template.AppendLine("| Action | Owner | Due Date | Priority |");
        template.AppendLine("|--------|-------|----------|----------|");
        template.AppendLine("| Implement additional monitoring | SRE Team | YYYY-MM-DD | High |");
        template.AppendLine("| Update runbook | On-call | YYYY-MM-DD | Medium |");
        template.AppendLine("| Add automated testing | Dev Team | YYYY-MM-DD | High |");
        template.AppendLine();
        template.AppendLine("## Lessons Learned");
        template.AppendLine("1. [Key learning #1]");
        template.AppendLine("2. [Key learning #2]");
        template.AppendLine("3. [Key learning #3]");
        
        return template.ToString();
    }

    private string DetermineIncidentSeverity(RequestAnalysis analysis, DevOpsContext context)
    {
        if (analysis.Urgency > 0.8 && context.Environment.IsProduction)
            return "Critical";
        if (analysis.Urgency > 0.6 || context.Environment.IsProduction)
            return "High";
        if (analysis.Urgency > 0.4)
            return "Medium";
        
        return "Low";
    }

    private Dictionary<string, object> AnalyzePerformanceMetrics(DevOpsContext context)
    {
        return new Dictionary<string, object>
        {
            ["response_time_target"] = context.Performance.MaxResponseTimeMs,
            ["cpu_threshold"] = context.Performance.MaxCpuPercent,
            ["memory_limit"] = context.Performance.MaxMemoryMb,
            ["concurrent_requests"] = context.Performance.MaxConcurrentRequests,
            ["performance_grade"] = DeterminePerformanceGrade(context)
        };
    }

    private string DeterminePerformanceGrade(DevOpsContext context)
    {
        if (context.Performance.MaxResponseTimeMs <= 100 && context.Performance.MaxCpuPercent <= 60)
            return "Excellent";
        if (context.Performance.MaxResponseTimeMs <= 500 && context.Performance.MaxCpuPercent <= 80)
            return "Good";
        if (context.Performance.MaxResponseTimeMs <= 1000)
            return "Fair";
        
        return "Needs Improvement";
    }

    private double CalculateReliabilityScore(DevOpsContext context)
    {
        var score = 0.95; // Base score
        
        if (context.Environment.IsProduction)
            score += 0.02;
        
        if (context.Security.RequiresMfa)
            score += 0.01;
        
        if (context.TechStack.Tools.Contains("Kubernetes"))
            score += 0.01;
        
        if (context.Performance.MaxConcurrentRequests > 1000)
            score -= 0.02; // Higher load = more risk
        
        return Math.Min(0.9999, Math.Max(0.9, score));
    }

    private double CalculateErrorBudget(double reliabilityScore)
    {
        var targetSLO = 0.999; // 99.9%
        var monthlyMinutes = 30 * 24 * 60; // Minutes in a month
        var allowedDowntime = monthlyMinutes * (1 - targetSLO);
        var actualDowntime = monthlyMinutes * (1 - reliabilityScore);
        
        return Math.Max(0, allowedDowntime - actualDowntime);
    }

    private double CalculateScalingNeeds(DevOpsContext context)
    {
        // Simple scaling calculation - can be enhanced with ML
        var currentLoad = context.Performance.MaxConcurrentRequests;
        var capacityUtilization = (double)currentLoad / 1000; // Assume 1000 is ideal capacity
        
        if (capacityUtilization > 0.8)
            return 2.0; // Need to double capacity
        if (capacityUtilization > 0.6)
            return 1.5; // Need 50% more capacity
        
        return 1.0; // No scaling needed
    }

    private List<SuggestedAction> GenerateSREActions(RequestAnalysis analysis, DevOpsContext context)
    {
        var actions = new List<SuggestedAction>();

        if (analysis.Intent == "Incident Response")
        {
            actions.Add(new SuggestedAction
            {
                Title = "Create Incident Timeline",
                Description = "Document all events with timestamps",
                Priority = ActionPriority.Critical,
                Category = "Incident Management"
            });
            
            actions.Add(new SuggestedAction
            {
                Title = "Gather Diagnostics",
                Description = "Collect logs, metrics, and traces",
                Priority = ActionPriority.High,
                Category = "Troubleshooting"
            });
        }

        if (analysis.Topics.Contains("Performance"))
        {
            actions.Add(new SuggestedAction
            {
                Title = "Run Performance Profile",
                Description = "Execute performance profiling tools",
                Priority = ActionPriority.High,
                Category = "Performance"
            });
        }

        if (context.Environment.IsProduction && !analysis.Topics.Contains("Monitoring"))
        {
            actions.Add(new SuggestedAction
            {
                Title = "Review Monitoring Coverage",
                Description = "Ensure all critical paths are monitored",
                Priority = ActionPriority.Medium,
                Category = "Observability"
            });
        }

        return actions;
    }

    private PersonaConfidence CalculateSREConfidence(RequestAnalysis analysis, DevOpsContext context)
    {
        var confidence = new PersonaConfidence
        {
            DomainExpertise = 0.95, // SRE is our specialty
            ContextRelevance = CalculateSREContextRelevance(analysis, context),
            ResponseQuality = 0.9
        };

        confidence.Overall = (confidence.DomainExpertise * 0.4 + 
                            confidence.ContextRelevance * 0.3 + 
                            confidence.ResponseQuality * 0.3);

        if (analysis.Intent == "Incident Response" && analysis.Urgency > 0.8)
        {
            confidence.Caveats.Add("Initial response based on limited information - gather more data");
        }

        if (!context.TechStack.Tools.Any())
        {
            confidence.Caveats.Add("Recommendations based on general best practices - adjust for your stack");
        }

        return confidence;
    }

    private double CalculateSREContextRelevance(RequestAnalysis analysis, DevOpsContext context)
    {
        var relevance = 0.6;

        if (analysis.Topics.Any(t => t.Contains("Reliability") || t.Contains("Performance")))
            relevance += 0.2;

        if (context.TechStack.Tools.Contains("Kubernetes") || context.TechStack.Tools.Contains("Prometheus"))
            relevance += 0.15;

        if (context.Environment.IsProduction)
            relevance += 0.05;

        return Math.Min(1.0, relevance);
    }
}