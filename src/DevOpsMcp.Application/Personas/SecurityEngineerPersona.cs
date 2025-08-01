using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Logging;
using MediatR;

namespace DevOpsMcp.Application.Personas;

public class SecurityEngineerPersona : BaseDevOpsPersona
{
    private readonly IMediator _mediator;
    private const string PersonaId = "security-engineer";
    
    public SecurityEngineerPersona(
        ILogger<SecurityEngineerPersona> logger,
        IPersonaMemoryManager memoryManager,
        IMediator mediator) 
        : base(logger, memoryManager)
    {
        _mediator = mediator;
        Initialize();
    }

    public override string Id => PersonaId;
    public override string Name => "Security Engineer";
    public override string Role => "DevOps Security Specialist";
    public override string Description => "Expert in security practices, vulnerability management, compliance, threat detection, and secure development lifecycle";
    public override DevOpsSpecialization Specialization => DevOpsSpecialization.Security;

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
            ["security_tools"] = new List<string> { "Azure Security Center", "OWASP ZAP", "SonarQube", "Snyk", "Vault", "Falco" },
            ["compliance_frameworks"] = new List<string> { "SOC 2", "ISO 27001", "PCI DSS", "HIPAA", "GDPR", "CIS" },
            ["security_practices"] = new List<string> { "SAST", "DAST", "Pen Testing", "Threat Modeling", "Security Reviews" },
            ["identity_management"] = new List<string> { "OAuth 2.0", "OIDC", "SAML", "Azure AD", "Keycloak", "MFA" },
            ["encryption"] = new List<string> { "TLS", "PKI", "Key Management", "HSM", "Certificate Management" },
            ["container_security"] = new List<string> { "Image Scanning", "Policy Enforcement", "Runtime Protection", "Admission Controllers" },
            ["network_security"] = new List<string> { "Zero Trust", "Micro-segmentation", "WAF", "DDoS Protection", "VPN" },
            ["incident_response"] = new List<string> { "SIEM", "Forensics", "Threat Hunting", "Incident Management" }
        };
    }

    protected override async Task<RequestAnalysis> AnalyzeRequestAsync(string request, DevOpsContext context)
    {
        var analysis = new RequestAnalysis
        {
            Intent = DetermineSecurityIntent(request),
            Urgency = CalculateSecurityUrgency(request, context),
            EstimatedCategory = TaskCategory.Security
        };

        // Add topics
        foreach (var topic in ExtractSecurityTopics(request))
        {
            analysis.Topics.Add(topic);
        }
        
        // Add context
        analysis.Context["environment"] = context.Environment.EnvironmentType;
        analysis.Context["compliance_required"] = context.Compliance.RequiredFrameworks.Any().ToString();
        analysis.Context["threat_level"] = AssessThreatLevel(context).ToString();

        // Extract entities
        var entities = ExtractSecurityEntities(request);
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
                ResponseType = "Security Guidance",
            }
        };

        switch (analysis.Intent)
        {
            case "Vulnerability Assessment":
                response = await GenerateVulnerabilityResponse(analysis, context);
                break;
            case "Compliance Review":
                response = await GenerateComplianceResponse(analysis, context);
                break;
            case "Security Architecture":
                response = await GenerateArchitectureResponse(analysis, context);
                break;
            case "Incident Response":
                response = await GenerateIncidentResponse(analysis, context);
                break;
            case "Access Control":
                response = await GenerateAccessControlResponse(analysis, context);
                break;
            case "Security Automation":
                response = await GenerateAutomationResponse(analysis, context);
                break;
            default:
                response = await GenerateGeneralSecurityResponse(analysis, context);
                break;
        }

        // Add topics to response
        foreach (var topic in analysis.Topics)
        {
            response.Metadata.Topics.Add(topic);
        }
        
        // Add suggested actions
        foreach (var action in GenerateSecurityActions(analysis, context))
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
            ["category"] = 0.4,
            ["skills"] = 0.3,
            ["complexity"] = 0.2,
            ["specialization"] = 0.1
        };
    }

    protected override Dictionary<TaskCategory, double> GetCategoryAlignmentMap()
    {
        return new Dictionary<TaskCategory, double>
        {
            [TaskCategory.Security] = 1.0,
            [TaskCategory.Monitoring] = 0.8,
            [TaskCategory.Infrastructure] = 0.7,
            [TaskCategory.Automation] = 0.7,
            [TaskCategory.Troubleshooting] = 0.6,
            [TaskCategory.Architecture] = 0.8,
            [TaskCategory.Deployment] = 0.5,
            [TaskCategory.Performance] = 0.4,
            [TaskCategory.Planning] = 0.6,
            [TaskCategory.Documentation] = 0.5
        };
    }

    protected override List<string> GetPersonaSkills()
    {
        return new List<string>
        {
            "Security Architecture", "Vulnerability Management", "Threat Modeling", "SAST/DAST",
            "Compliance", "Identity Management", "Encryption", "Network Security", "Container Security",
            "SIEM", "Incident Response", "Penetration Testing", "Security Automation", "Cloud Security",
            "Zero Trust", "DevSecOps", "Security Policies", "Risk Assessment"
        };
    }

    private string DetermineSecurityIntent(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("vulnerabilit") || requestLower.Contains("scan") || requestLower.Contains("cve"))
            return "Vulnerability Assessment";
        if (requestLower.Contains("compliance") || requestLower.Contains("audit") || requestLower.Contains("pci") || requestLower.Contains("hipaa"))
            return "Compliance Review";
        if (requestLower.Contains("architect") || requestLower.Contains("design") || requestLower.Contains("security model"))
            return "Security Architecture";
        if (requestLower.Contains("incident") || requestLower.Contains("breach") || requestLower.Contains("attack"))
            return "Incident Response";
        if (requestLower.Contains("access") || requestLower.Contains("permission") || requestLower.Contains("rbac") || requestLower.Contains("identity"))
            return "Access Control";
        if (requestLower.Contains("automat") || requestLower.Contains("policy") || requestLower.Contains("enforce"))
            return "Security Automation";

        return "General Security";
    }

    private List<string> ExtractSecurityTopics(string request)
    {
        var topics = new List<string>();
        var requestLower = request.ToLowerInvariant();

        var topicKeywords = new Dictionary<string, string[]>
        {
            ["Vulnerability Management"] = new[] { "vulnerability", "cve", "patch", "scan", "exploit" },
            ["Compliance"] = new[] { "compliance", "audit", "pci", "hipaa", "gdpr", "iso" },
            ["Identity & Access"] = new[] { "identity", "access", "authentication", "authorization", "rbac", "iam" },
            ["Container Security"] = new[] { "container", "docker", "kubernetes", "image", "pod security" },
            ["Network Security"] = new[] { "network", "firewall", "vpn", "zero trust", "segmentation" },
            ["Data Protection"] = new[] { "encryption", "data", "pii", "privacy", "key management" },
            ["Incident Response"] = new[] { "incident", "breach", "forensics", "siem", "alert" }
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

    private double CalculateSecurityUrgency(string request, DevOpsContext context)
    {
        var urgency = 0.6; // Default higher base urgency for security

        if (request.ToLowerInvariant().Contains("breach") || request.ToLowerInvariant().Contains("incident"))
            urgency = 1.0;

        if (request.ToLowerInvariant().Contains("critical") || request.ToLowerInvariant().Contains("zero-day"))
            urgency += 0.3;

        if (context.Environment.IsProduction)
            urgency += 0.2;

        if (context.Security.RecentIncidents > 0)
            urgency += 0.2;

        return Math.Min(1.0, urgency);
    }

    private string AssessThreatLevel(DevOpsContext context)
    {
        if (context.Security.RecentIncidents > 0 || context.Security.ThreatLevel == "High")
            return "High";
        if (context.Environment.IsProduction && context.Environment.IsExternallyAccessible)
            return "Medium";
        return "Low";
    }

    private Dictionary<string, object> ExtractSecurityEntities(string request)
    {
        var entities = new Dictionary<string, object>();

        // Extract security tools
        var tools = new List<string>();
        var toolKeywords = new[] { "sonarqube", "snyk", "vault", "falco", "zap", "trivy" };
        foreach (var tool in toolKeywords)
        {
            if (request.ToLowerInvariant().Contains(tool))
                tools.Add(tool);
        }
        if (tools.Any())
            entities["security_tools"] = tools;

        // Extract compliance standards
        var standards = new[] { "pci dss", "hipaa", "gdpr", "iso 27001", "soc 2" };
        var mentionedStandards = standards.Where(std => request.ToLowerInvariant().Contains(std)).ToList();
        if (mentionedStandards.Any())
            entities["compliance_standards"] = mentionedStandards;

        // Extract vulnerability types
        var vulnTypes = new[] { "sql injection", "xss", "csrf", "xxe", "ssrf", "rce" };
        var mentionedVulns = vulnTypes.Where(vuln => request.ToLowerInvariant().Contains(vuln)).ToList();
        if (mentionedVulns.Any())
            entities["vulnerability_types"] = mentionedVulns;

        return entities;
    }

    private async Task<PersonaResponse> GenerateVulnerabilityResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateVulnerabilityGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Vulnerability Assessment",
            }
        };
        
        response.Metadata.Topics.Add("Vulnerability Management");
        response.Metadata.Topics.Add("Security Scanning");

        response.SuggestedActions.Add(new SuggestedAction
        {
            Title = "Run Security Scan",
            Description = "Execute comprehensive vulnerability scanning across the infrastructure",
            Priority = ActionPriority.Critical,
            Category = "Security"
        });

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateComplianceResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateComplianceGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Compliance Review",
            }
        };
        
        response.Metadata.Topics.Add("Compliance");
        response.Metadata.Topics.Add("Regulatory Requirements");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateArchitectureResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateSecurityArchitectureGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Security Architecture",
            }
        };
        
        response.Metadata.Topics.Add("Security Architecture");
        response.Metadata.Topics.Add("Defense in Depth");

        return await Task.FromResult(response);
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
            }
        };
        
        response.Metadata.Topics.Add("Incident Response");
        response.Metadata.Topics.Add("Security Breach");

        response.SuggestedActions.Add(new SuggestedAction
        {
            Title = "Activate Incident Response Team",
            Description = "Notify security team and begin incident response procedures",
            Priority = ActionPriority.Critical,
            Category = "Incident Response"
        });

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateAccessControlResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateAccessControlGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Access Control",
            }
        };
        
        response.Metadata.Topics.Add("Identity & Access");
        response.Metadata.Topics.Add("Authentication");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateAutomationResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = GenerateSecurityAutomationGuidance(analysis, context),
            Metadata = new ResponseMetadata
            {
                ResponseType = "Security Automation",
            }
        };
        
        response.Metadata.Topics.Add("Security Automation");
        response.Metadata.Topics.Add("DevSecOps");

        return await Task.FromResult(response);
    }

    private async Task<PersonaResponse> GenerateGeneralSecurityResponse(RequestAnalysis analysis, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = "I'm here to help with your security concerns. Could you provide more specific details about the security challenge you're facing?",
            Metadata = new ResponseMetadata
            {
                ResponseType = "General Security Guidance",
            }
        };
        
        // Topics already added in main method from analysis.Topics

        return await Task.FromResult(response);
    }

    private string GenerateVulnerabilityGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Based on your vulnerability management requirements, here's my security assessment:");
        guidance.AppendLine();
        
        guidance.AppendLine("**Vulnerability Scanning Strategy:**");
        guidance.AppendLine("1. **Static Application Security Testing (SAST)**");
        guidance.AppendLine("   - Integrate SonarQube or Checkmarx in CI pipeline");
        guidance.AppendLine("   - Configure quality gates for security issues");
        guidance.AppendLine("   - Set thresholds for critical/high vulnerabilities");
        guidance.AppendLine();
        guidance.AppendLine("2. **Dynamic Application Security Testing (DAST)**");
        guidance.AppendLine("   - Deploy OWASP ZAP or Burp Suite for runtime testing");
        guidance.AppendLine("   - Schedule regular scans against staging environments");
        guidance.AppendLine("   - Automate API security testing");
        guidance.AppendLine();
        guidance.AppendLine("3. **Container Image Scanning**");
        guidance.AppendLine("   - Implement Trivy or Snyk for image vulnerability scanning");
        guidance.AppendLine("   - Block deployment of images with critical CVEs");
        guidance.AppendLine("   - Scan base images and dependencies");
        guidance.AppendLine();
        guidance.AppendLine("4. **Infrastructure Scanning**");
        guidance.AppendLine("   - Use Azure Security Center or AWS Security Hub");
        guidance.AppendLine("   - Enable continuous compliance monitoring");
        guidance.AppendLine("   - Configure automated remediation where possible");
        
        if (context.Environment.IsProduction)
        {
            guidance.AppendLine();
            guidance.AppendLine("**Production-Specific Considerations:**");
            guidance.AppendLine("- Implement virtual patching with WAF rules");
            guidance.AppendLine("- Prioritize zero-downtime patching strategies");
            guidance.AppendLine("- Maintain emergency response procedures");
        }
        
        return guidance.ToString();
    }

    private string GenerateComplianceGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Compliance assessment for your environment:");
        guidance.AppendLine();
        
        if (context.Compliance.RequiredFrameworks.Any())
        {
            guidance.AppendLine($"**Required Compliance Frameworks:** {string.Join(", ", context.Compliance.RequiredFrameworks)}");
            guidance.AppendLine();
            
            foreach (var framework in context.Compliance.RequiredFrameworks)
            {
                guidance.AppendLine($"**{framework} Requirements:**");
                switch (framework.ToUpperInvariant())
                {
                    case "PCI DSS":
                        guidance.AppendLine("- Implement network segmentation");
                        guidance.AppendLine("- Enable audit logging for all access");
                        guidance.AppendLine("- Encrypt cardholder data at rest and in transit");
                        guidance.AppendLine("- Implement strong access controls");
                        guidance.AppendLine("- Regular security testing and monitoring");
                        break;
                    case "HIPAA":
                        guidance.AppendLine("- Ensure PHI encryption (AES-256 minimum)");
                        guidance.AppendLine("- Implement comprehensive audit trails");
                        guidance.AppendLine("- Configure automatic logoff policies");
                        guidance.AppendLine("- Enable data backup and disaster recovery");
                        guidance.AppendLine("- Maintain Business Associate Agreements");
                        break;
                    case "GDPR":
                        guidance.AppendLine("- Implement data privacy by design");
                        guidance.AppendLine("- Enable right to erasure mechanisms");
                        guidance.AppendLine("- Configure data portability features");
                        guidance.AppendLine("- Maintain processing activity records");
                        guidance.AppendLine("- Implement consent management");
                        break;
                }
                guidance.AppendLine();
            }
        }
        
        guidance.AppendLine("**Compliance Automation Tools:**");
        guidance.AppendLine("- Azure Policy / AWS Config for continuous compliance");
        guidance.AppendLine("- Chef InSpec or Open Policy Agent for policy as code");
        guidance.AppendLine("- Automated compliance reporting dashboards");
        
        return guidance.ToString();
    }

    private string GenerateSecurityArchitectureGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Security architecture recommendations for your system:");
        guidance.AppendLine();
        guidance.AppendLine("**Defense in Depth Strategy:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Network Layer Security**");
        guidance.AppendLine("   - Implement Zero Trust network architecture");
        guidance.AppendLine("   - Deploy micro-segmentation with network policies");
        guidance.AppendLine("   - Configure WAF with custom rule sets");
        guidance.AppendLine("   - Enable DDoS protection");
        guidance.AppendLine();
        guidance.AppendLine("2. **Application Layer Security**");
        guidance.AppendLine("   - Implement secure coding practices");
        guidance.AppendLine("   - Use parameterized queries to prevent SQL injection");
        guidance.AppendLine("   - Enable CSRF tokens and security headers");
        guidance.AppendLine("   - Implement rate limiting and throttling");
        guidance.AppendLine();
        guidance.AppendLine("3. **Data Layer Security**");
        guidance.AppendLine("   - Encrypt data at rest using AES-256");
        guidance.AppendLine("   - Implement TLS 1.3 for data in transit");
        guidance.AppendLine("   - Use Hardware Security Modules (HSM) for key management");
        guidance.AppendLine("   - Enable database audit logging");
        guidance.AppendLine();
        guidance.AppendLine("4. **Identity and Access Management**");
        guidance.AppendLine("   - Implement strong authentication (MFA required)");
        guidance.AppendLine("   - Use RBAC with principle of least privilege");
        guidance.AppendLine("   - Enable Just-In-Time (JIT) access");
        guidance.AppendLine("   - Implement service account governance");
        
        if (Configuration.SecurityPosture == SecurityPosture.Strict)
        {
            guidance.AppendLine();
            guidance.AppendLine("**Enhanced Security Measures:**");
            guidance.AppendLine("- Implement runtime application self-protection (RASP)");
            guidance.AppendLine("- Deploy deception technology (honeypots)");
            guidance.AppendLine("- Enable advanced threat detection with ML");
            guidance.AppendLine("- Implement security chaos engineering");
        }
        
        return guidance.ToString();
    }

    private string GenerateIncidentResponsePlan(RequestAnalysis analysis, DevOpsContext context)
    {
        var plan = new System.Text.StringBuilder();
        var urgency = analysis.Urgency > 0.8 ? "CRITICAL" : "STANDARD";
        
        plan.AppendLine($"**{urgency} INCIDENT RESPONSE PLAN**");
        plan.AppendLine();
        plan.AppendLine("**IMMEDIATE ACTIONS (0-15 minutes):**");
        plan.AppendLine("1. **Assess and Contain**");
        plan.AppendLine("   - Identify affected systems and scope of incident");
        plan.AppendLine("   - Isolate compromised systems from network");
        plan.AppendLine("   - Preserve evidence (memory dumps, logs)");
        plan.AppendLine("   - Activate incident response team");
        plan.AppendLine();
        plan.AppendLine("2. **Initial Communication**");
        plan.AppendLine("   - Notify security team and management");
        plan.AppendLine("   - Create incident ticket with severity level");
        plan.AppendLine("   - Establish secure communication channel");
        plan.AppendLine();
        plan.AppendLine("**SHORT-TERM ACTIONS (15 min - 2 hours):**");
        plan.AppendLine("3. **Investigation**");
        plan.AppendLine("   - Collect and analyze logs from SIEM");
        plan.AppendLine("   - Identify attack vectors and IOCs");
        plan.AppendLine("   - Determine data exposure and impact");
        plan.AppendLine("   - Check for lateral movement");
        plan.AppendLine();
        plan.AppendLine("4. **Mitigation**");
        plan.AppendLine("   - Apply emergency patches or configurations");
        plan.AppendLine("   - Reset compromised credentials");
        plan.AppendLine("   - Update firewall and WAF rules");
        plan.AppendLine("   - Enable enhanced monitoring");
        plan.AppendLine();
        plan.AppendLine("**RECOVERY ACTIONS (2-24 hours):**");
        plan.AppendLine("5. **Restoration**");
        plan.AppendLine("   - Rebuild affected systems from clean backups");
        plan.AppendLine("   - Verify system integrity");
        plan.AppendLine("   - Conduct vulnerability assessment");
        plan.AppendLine("   - Gradually restore services");
        plan.AppendLine();
        plan.AppendLine("6. **Validation**");
        plan.AppendLine("   - Perform security testing");
        plan.AppendLine("   - Monitor for recurring indicators");
        plan.AppendLine("   - Validate all security controls");
        plan.AppendLine();
        plan.AppendLine("**POST-INCIDENT (24+ hours):**");
        plan.AppendLine("7. **Documentation and Learning**");
        plan.AppendLine("   - Complete incident report");
        plan.AppendLine("   - Conduct post-mortem analysis");
        plan.AppendLine("   - Update security procedures");
        plan.AppendLine("   - Share threat intelligence");
        
        if (context.Environment.IsRegulated)
        {
            plan.AppendLine();
            plan.AppendLine("**Regulatory Requirements:**");
            plan.AppendLine("- Notify compliance officer within 1 hour");
            plan.AppendLine("- Prepare breach notification if required");
            plan.AppendLine("- Document all actions for audit trail");
        }
        
        return plan.ToString();
    }

    private string GenerateAccessControlGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Access control and identity management recommendations:");
        guidance.AppendLine();
        guidance.AppendLine("**Identity Architecture:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Authentication Strategy**");
        guidance.AppendLine("   - Implement SSO with SAML 2.0 or OIDC");
        guidance.AppendLine("   - Enforce MFA for all users (TOTP/FIDO2)");
        guidance.AppendLine("   - Use certificate-based auth for services");
        guidance.AppendLine("   - Implement adaptive authentication");
        guidance.AppendLine();
        guidance.AppendLine("2. **Authorization Model**");
        guidance.AppendLine("   - Design RBAC with clear role definitions");
        guidance.AppendLine("   - Implement attribute-based access control (ABAC)");
        guidance.AppendLine("   - Use policy engines for complex rules");
        guidance.AppendLine("   - Enable dynamic authorization");
        guidance.AppendLine();
        guidance.AppendLine("3. **Privileged Access Management**");
        guidance.AppendLine("   - Implement Just-In-Time (JIT) access");
        guidance.AppendLine("   - Use privileged access workstations");
        guidance.AppendLine("   - Enable session recording for admin access");
        guidance.AppendLine("   - Rotate service account credentials");
        guidance.AppendLine();
        guidance.AppendLine("4. **Access Governance**");
        guidance.AppendLine("   - Regular access reviews (quarterly)");
        guidance.AppendLine("   - Automated de-provisioning");
        guidance.AppendLine("   - Segregation of duties enforcement");
        guidance.AppendLine("   - Access certification workflows");
        
        if (context.TechStack.CloudProvider == "Azure")
        {
            guidance.AppendLine();
            guidance.AppendLine("**Azure-Specific Recommendations:**");
            guidance.AppendLine("- Use Azure AD Conditional Access policies");
            guidance.AppendLine("- Implement Privileged Identity Management (PIM)");
            guidance.AppendLine("- Enable Identity Protection risk policies");
            guidance.AppendLine("- Use Managed Identities for Azure resources");
        }
        
        return guidance.ToString();
    }

    private string GenerateSecurityAutomationGuidance(RequestAnalysis analysis, DevOpsContext context)
    {
        var guidance = new System.Text.StringBuilder();
        
        guidance.AppendLine("Security automation and DevSecOps implementation:");
        guidance.AppendLine();
        guidance.AppendLine("**Security Pipeline Integration:**");
        guidance.AppendLine();
        guidance.AppendLine("1. **Pre-Commit Security**");
        guidance.AppendLine("   - Git hooks for secret scanning");
        guidance.AppendLine("   - IDE security plugins");
        guidance.AppendLine("   - Local SAST checks");
        guidance.AppendLine();
        guidance.AppendLine("2. **CI/CD Security Gates**");
        guidance.AppendLine("   ```yaml");
        guidance.AppendLine("   - stage: SecurityScan");
        guidance.AppendLine("     jobs:");
        guidance.AppendLine("       - SAST with SonarQube");
        guidance.AppendLine("       - Dependency scanning with Snyk");
        guidance.AppendLine("       - Container scanning with Trivy");
        guidance.AppendLine("       - License compliance check");
        guidance.AppendLine("       - Security unit tests");
        guidance.AppendLine("   ```");
        guidance.AppendLine();
        guidance.AppendLine("3. **Policy as Code**");
        guidance.AppendLine("   - Open Policy Agent for runtime policies");
        guidance.AppendLine("   - Kubernetes admission controllers");
        guidance.AppendLine("   - Cloud security policies (Azure Policy/AWS Config)");
        guidance.AppendLine("   - Infrastructure compliance scanning");
        guidance.AppendLine();
        guidance.AppendLine("4. **Automated Response**");
        guidance.AppendLine("   - Auto-remediation for common issues");
        guidance.AppendLine("   - Automated patch deployment");
        guidance.AppendLine("   - Dynamic firewall rule updates");
        guidance.AppendLine("   - Automated incident containment");
        guidance.AppendLine();
        guidance.AppendLine("5. **Security Observability**");
        guidance.AppendLine("   - Centralized security logging");
        guidance.AppendLine("   - Real-time threat detection");
        guidance.AppendLine("   - Security metrics dashboards");
        guidance.AppendLine("   - Automated compliance reporting");
        
        return guidance.ToString();
    }

    private List<SuggestedAction> GenerateSecurityActions(RequestAnalysis analysis, DevOpsContext context)
    {
        var actions = new List<SuggestedAction>();

        if (analysis.Intent == "Incident Response")
        {
            actions.Add(new SuggestedAction
            {
                Title = "Activate Security Incident Response",
                Description = "Begin immediate incident response procedures",
                Priority = ActionPriority.Critical,
                Category = "Security"
            });
        }

        if (context.Security.LastSecurityScan == null || 
            (DateTime.UtcNow - context.Security.LastSecurityScan.Value).TotalDays > 7)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Run Security Scan",
                Description = "Execute comprehensive security assessment",
                Priority = ActionPriority.High,
                Category = "Security"
            });
        }

        if (context.Compliance.RequiredFrameworks.Any())
        {
            actions.Add(new SuggestedAction
            {
                Title = "Compliance Validation",
                Description = "Verify compliance with required frameworks",
                Priority = ActionPriority.High,
                Category = "Compliance"
            });
        }

        if (!context.Security.MfaEnabled)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Enable Multi-Factor Authentication",
                Description = "Implement MFA for all user accounts",
                Priority = ActionPriority.Critical,
                Category = "Identity"
            });
        }

        return actions;
    }

    private PersonaConfidence CalculateResponseConfidence(RequestAnalysis analysis, DevOpsContext context)
    {
        var confidence = new PersonaConfidence
        {
            DomainExpertise = 0.95, // Security is our specialty
            ContextRelevance = CalculateContextRelevance(analysis, context),
            ResponseQuality = 0.9
        };

        confidence.Overall = (confidence.DomainExpertise * 0.4 + 
                            confidence.ContextRelevance * 0.3 + 
                            confidence.ResponseQuality * 0.3);

        // Add security-specific caveats
        if (context.Security.UnknownAssets)
        {
            confidence.Caveats.Add("Complete asset inventory required for comprehensive assessment");
        }

        if (analysis.Urgency > 0.9)
        {
            confidence.Caveats.Add("Emergency response - follow up with detailed security audit");
        }

        if (context.Compliance.RequiredFrameworks.Contains("PCI DSS") || 
            context.Compliance.RequiredFrameworks.Contains("HIPAA"))
        {
            confidence.Caveats.Add("Consult with compliance team for regulatory requirements");
        }

        return confidence;
    }

    private double CalculateContextRelevance(RequestAnalysis analysis, DevOpsContext context)
    {
        var relevance = 0.7; // Higher base relevance for security

        if (context.Security.SecurityToolsInUse.Any(tool => 
            Capabilities["security_tools"] is List<string> tools && 
            tools.Contains(tool, StringComparer.OrdinalIgnoreCase)))
        {
            relevance += 0.2;
        }

        if (analysis.Topics.Any(t => Capabilities.ContainsKey(t.ToLowerInvariant().Replace(" ", "_"))))
            relevance += 0.1;

        return Math.Min(1.0, relevance);
    }
}