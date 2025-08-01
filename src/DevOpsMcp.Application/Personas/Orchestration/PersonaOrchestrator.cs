using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Orchestration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace DevOpsMcp.Application.Personas.Orchestration;

public class PersonaOrchestrator : IPersonaOrchestrator
{
    private readonly ILogger<PersonaOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IDevOpsPersona> _personas;
    private readonly ConcurrentDictionary<string, PersonaStatus> _personaStatuses;
    private readonly Dictionary<string, int> _roundRobinCounters;
    private readonly object _lockObject = new();

    public PersonaOrchestrator(
        ILogger<PersonaOrchestrator> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _personas = new ConcurrentDictionary<string, IDevOpsPersona>();
        _personaStatuses = new ConcurrentDictionary<string, PersonaStatus>();
        _roundRobinCounters = new Dictionary<string, int>();
        
        InitializePersonas();
    }

    public async Task<PersonaSelectionResult> SelectPersonaAsync(
        DevOpsContext context,
        string request,
        PersonaSelectionCriteria criteria)
    {
        _logger.LogInformation("Selecting persona for request with mode: {Mode}", criteria.SelectionMode);

        var result = new PersonaSelectionResult();
        var activePersonas = await GetActivePersonasAsync();

        if (!activePersonas.Any())
        {
            throw new System.InvalidOperationException("No active personas available");
        }

        switch (criteria.SelectionMode)
        {
            case PersonaSelectionMode.BestMatch:
                result = await SelectBestMatchPersonaAsync(context, request, criteria, activePersonas);
                break;
                
            case PersonaSelectionMode.RoundRobin:
                result = SelectRoundRobinPersona(activePersonas);
                break;
                
            case PersonaSelectionMode.LoadBalanced:
                result = SelectLoadBalancedPersona(activePersonas);
                break;
                
            case PersonaSelectionMode.SpecializationBased:
                result = await SelectSpecializationBasedPersonaAsync(context, request, criteria, activePersonas);
                break;
                
            case PersonaSelectionMode.ContextAware:
                result = await SelectContextAwarePersonaAsync(context, request, criteria, activePersonas);
                break;
        }

        _logger.LogInformation("Selected persona: {PersonaId} with confidence: {Confidence}", 
            result.PrimaryPersonaId, result.Confidence);

        return result;
    }

    public async Task<OrchestrationResult> OrchestrateMultiPersonaResponseAsync(
        DevOpsContext context,
        string request,
        List<string> involvedPersonaIds)
    {
        _logger.LogInformation("Orchestrating response with {Count} personas", involvedPersonaIds.Count);

        var stopwatch = Stopwatch.StartNew();
        var result = new OrchestrationResult();

        // Get responses from all involved personas in parallel
        var responseTasks = involvedPersonaIds.Select(async personaId =>
        {
            var personaStopwatch = Stopwatch.StartNew();
            try
            {
                var response = await RouteRequestAsync(personaId, context, request);
                personaStopwatch.Stop();
                
                result.Metrics.PersonaDurations[personaId] = personaStopwatch.ElapsedMilliseconds;
                
                return new PersonaContribution
                {
                    PersonaId = personaId,
                    Response = response,
                    Weight = CalculateContributionWeight(personaId, response, context),
                    Type = DetermineContributionType(personaId, involvedPersonaIds)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting response from persona {PersonaId}", personaId);
                return null;
            }
        }).ToList();

        var contributions = (await Task.WhenAll(responseTasks))
            .Where(c => c != null)
            .Cast<PersonaContribution>()
            .ToList();

        result.Contributions.AddRange(contributions);

        // Consolidate responses
        result.ConsolidatedResponse = await ConsolidateResponsesAsync(contributions, context);

        // Combine context from all responses
        foreach (var contribution in contributions)
        {
            foreach (var contextItem in contribution.Response.Context)
            {
                result.CombinedContext[$"{contribution.PersonaId}_{contextItem.Key}"] = contextItem.Value;
            }
        }

        // Calculate metrics
        stopwatch.Stop();
        result.Metrics.TotalDuration = stopwatch.ElapsedMilliseconds;
        result.Metrics.OverallConfidence = contributions.Average(c => c.Response.Confidence.Overall);

        _logger.LogInformation("Orchestration completed in {Duration}ms with confidence {Confidence}", 
            result.Metrics.TotalDuration, result.Metrics.OverallConfidence);

        return result;
    }

    public async Task<PersonaResponse> RouteRequestAsync(
        string personaId,
        DevOpsContext context,
        string request)
    {
        _logger.LogDebug("Routing request to persona {PersonaId}", personaId);

        if (!_personas.TryGetValue(personaId, out var persona))
        {
            throw new ArgumentException($"Persona {personaId} not found");
        }

        // Update persona status
        if (_personaStatuses.TryGetValue(personaId, out var status))
        {
            status.CurrentLoad++;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await persona.ProcessRequestAsync(context, request);
            stopwatch.Stop();

            // Update metrics
            if (status != null)
            {
                status.AverageResponseTime = (status.AverageResponseTime * 0.9) + (stopwatch.ElapsedMilliseconds * 0.1);
                status.Health.SuccessRate = (status.Health.SuccessRate * 0.95) + 0.05;
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request with persona {PersonaId}", personaId);
            
            if (status != null)
            {
                status.Health.ErrorCount++;
                status.Health.LastError = DateTime.UtcNow;
                status.Health.SuccessRate = (status.Health.SuccessRate * 0.95);
                UpdateHealthStatus(status);
            }
            
            throw;
        }
        finally
        {
            if (status != null)
            {
                status.CurrentLoad--;
            }
        }
    }

    public async Task<ConflictResolution> ResolveConflictsAsync(
        List<PersonaResponse> responses,
        ConflictResolutionStrategy strategy)
    {
        _logger.LogInformation("Resolving conflicts between {Count} responses using {Strategy} strategy", 
            responses.Count, strategy);

        var resolution = new ConflictResolution
        {
            ResolutionMethod = strategy.ToString()
        };

        // Identify conflicts
        var conflicts = IdentifyConflicts(responses);
        foreach (var conflict in conflicts)
            resolution.ConflictingElements[conflict.Key] = conflict.Value;

        switch (strategy)
        {
            case ConflictResolutionStrategy.Consensus:
                resolution = await ResolveByConsensusAsync(responses, conflicts);
                break;
                
            case ConflictResolutionStrategy.HighestConfidence:
                resolution = ResolveByHighestConfidence(responses);
                break;
                
            case ConflictResolutionStrategy.SpecializationPriority:
                resolution = ResolveBySpecializationPriority(responses);
                break;
                
            case ConflictResolutionStrategy.WeightedAverage:
                resolution = ResolveByWeightedAverage(responses);
                break;
                
            case ConflictResolutionStrategy.UserPreference:
                resolution = ResolveByUserPreference(responses);
                break;
        }

        _logger.LogInformation("Resolved {Count} conflicts with confidence {Confidence}", 
            conflicts.Count, resolution.Confidence);

        return resolution;
    }

    public async Task<List<PersonaStatus>> GetActivePersonasAsync()
    {
        return await Task.FromResult(
            _personaStatuses.Values
                .Where(s => s.IsActive)
                .ToList()
        );
    }

    public async Task<bool> SetPersonaStatusAsync(string personaId, bool isActive)
    {
        _logger.LogInformation("Setting persona {PersonaId} status to {Status}", personaId, isActive);

        if (_personaStatuses.TryGetValue(personaId, out var status))
        {
            status.IsActive = isActive;
            status.LastActivated = isActive ? DateTime.UtcNow : status.LastActivated;
            
            return await Task.FromResult(true);
        }

        return await Task.FromResult(false);
    }

    private void InitializePersonas()
    {
        // Initialize default personas
        var personaTypes = new[]
        {
            ("devops-engineer", typeof(DevOpsEngineerPersona)),
            ("sre-specialist", typeof(SiteReliabilityEngineerPersona)),
            ("security-engineer", typeof(SecurityEngineerPersona)),
            ("engineering-manager", typeof(EngineeringManagerPersona))
        };

        foreach (var (id, type) in personaTypes)
        {
            try
            {
                var persona = (IDevOpsPersona?)_serviceProvider.GetService(type);
                if (persona != null)
                {
                    _personas[id] = persona;
                    _personaStatuses[id] = new PersonaStatus
                    {
                        PersonaId = id,
                        IsActive = true,
                        LastActivated = DateTime.UtcNow,
                        Health = new PersonaHealth
                        {
                            Status = HealthStatus.Healthy,
                            SuccessRate = 1.0,
                            AverageSatisfaction = 0.8
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize persona {PersonaId}", id);
            }
        }
    }

    private async Task<PersonaSelectionResult> SelectBestMatchPersonaAsync(
        DevOpsContext context,
        string request,
        PersonaSelectionCriteria criteria,
        List<PersonaStatus> activePersonas)
    {
        var result = new PersonaSelectionResult();
        var scoreTasks = new List<Task<(string personaId, double score)>>();

        foreach (var status in activePersonas)
        {
            if (_personas.TryGetValue(status.PersonaId, out var persona))
            {
                scoreTasks.Add(Task.Run(async () =>
                {
                    // Create a simple task to calculate alignment
                    var task = new DevOpsTask
                    {
                        Description = request,
                        Category = InferTaskCategory(request),
                        Complexity = InferComplexity(request)
                    };
                    
                    var score = await persona.CalculateRoleAlignmentAsync(task);
                    return (status.PersonaId, score);
                }));
            }
        }

        var scores = await Task.WhenAll(scoreTasks);
        
        foreach (var (personaId, score) in scores)
        {
            result.PersonaScores[personaId] = score;
        }

        var bestMatch = scores.OrderByDescending(s => s.score).FirstOrDefault();
        
        if (bestMatch.score >= criteria.MinimumConfidenceThreshold)
        {
            result.PrimaryPersonaId = bestMatch.personaId;
            result.Confidence = bestMatch.score;
            result.SelectionReason = $"Best match with score {bestMatch.score:F2}";
            
            // Add secondary personas if allowed
            if (criteria.AllowMultiplePersonas)
            {
                var secondaryMatches = scores
                    .Where(s => s.personaId != bestMatch.personaId && s.score >= criteria.MinimumConfidenceThreshold * 0.8)
                    .OrderByDescending(s => s.score)
                    .Take(criteria.MaxPersonaCount - 1)
                    .Select(s => s.personaId);
                    
                result.SecondaryPersonaIds.AddRange(secondaryMatches);
            }
        }
        else
        {
            // Fallback to highest score even if below threshold
            result.PrimaryPersonaId = bestMatch.personaId;
            result.Confidence = bestMatch.score;
            result.SelectionReason = $"No persona met threshold, selected highest score {bestMatch.score:F2}";
        }

        return result;
    }

    private PersonaSelectionResult SelectRoundRobinPersona(List<PersonaStatus> activePersonas)
    {
        lock (_lockObject)
        {
            var key = "round_robin";
            if (!_roundRobinCounters.TryGetValue(key, out var counter))
            {
                counter = 0;
                _roundRobinCounters[key] = counter;
            }

            var index = counter % activePersonas.Count;
            _roundRobinCounters[key] = counter + 1;

            var selected = activePersonas[index];

            return new PersonaSelectionResult
            {
                PrimaryPersonaId = selected.PersonaId,
                Confidence = 0.7, // Fixed confidence for round-robin
                SelectionReason = "Round-robin selection"
            };
        }
    }

    private PersonaSelectionResult SelectLoadBalancedPersona(List<PersonaStatus> activePersonas)
    {
        var selected = activePersonas
            .Where(p => p.Health.Status != HealthStatus.Unhealthy)
            .OrderBy(p => p.CurrentLoad)
            .ThenBy(p => p.AverageResponseTime)
            .FirstOrDefault();

        if (selected == null)
        {
            selected = activePersonas.First(); // Fallback
        }

        return new PersonaSelectionResult
        {
            PrimaryPersonaId = selected.PersonaId,
            Confidence = 0.8,
            SelectionReason = $"Load balanced selection (current load: {selected.CurrentLoad})"
        };
    }

    private async Task<PersonaSelectionResult> SelectSpecializationBasedPersonaAsync(
        DevOpsContext context,
        string request,
        PersonaSelectionCriteria criteria,
        List<PersonaStatus> activePersonas)
    {
        var result = new PersonaSelectionResult();
        var specialization = InferSpecialization(request);

        foreach (var status in activePersonas)
        {
            if (_personas.TryGetValue(status.PersonaId, out var persona))
            {
                if (criteria.PreferredSpecializations.Contains(persona.Specialization) ||
                    persona.Specialization == specialization)
                {
                    result.PrimaryPersonaId = status.PersonaId;
                    result.Confidence = 0.9;
                    result.SelectionReason = $"Specialization match: {persona.Specialization}";
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(result.PrimaryPersonaId))
        {
            // Fallback to best match
            result = await SelectBestMatchPersonaAsync(context, request, criteria, activePersonas);
        }

        return result;
    }

    private async Task<PersonaSelectionResult> SelectContextAwarePersonaAsync(
        DevOpsContext context,
        string request,
        PersonaSelectionCriteria criteria,
        List<PersonaStatus> activePersonas)
    {
        var result = new PersonaSelectionResult();

        // Consider context factors
        var contextScore = new Dictionary<string, double>();

        foreach (var status in activePersonas)
        {
            if (_personas.TryGetValue(status.PersonaId, out var persona))
            {
                var score = 0.0;

                // Environment context
                if (context.Environment.IsProduction && persona.Specialization == DevOpsSpecialization.Reliability)
                    score += 0.3;

                // Security context
                if (context.Security.RequiresMfa && persona.Specialization == DevOpsSpecialization.Security)
                    score += 0.3;

                // Team context
                if (context.Team.TeamSize > 20 && persona.Specialization == DevOpsSpecialization.Management)
                    score += 0.2;

                // Performance context
                if (context.Performance.MaxConcurrentRequests > 1000 && 
                    (persona.Specialization == DevOpsSpecialization.Reliability || 
                     persona.Specialization == DevOpsSpecialization.Infrastructure))
                    score += 0.2;

                contextScore[status.PersonaId] = score;
            }
        }

        var bestContextMatch = contextScore.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        
        if (bestContextMatch.Value > 0.5)
        {
            result.PrimaryPersonaId = bestContextMatch.Key;
            result.Confidence = bestContextMatch.Value;
            result.SelectionReason = $"Context-aware selection with score {bestContextMatch.Value:F2}";
        }
        else
        {
            // Fallback to best match
            result = await SelectBestMatchPersonaAsync(context, request, criteria, activePersonas);
        }

        return result;
    }

    private double CalculateContributionWeight(string personaId, PersonaResponse response, DevOpsContext context)
    {
        var weight = response.Confidence.Overall;

        // Adjust weight based on persona specialization and context
        if (_personas.TryGetValue(personaId, out var persona))
        {
            if (context.Environment.IsProduction && persona.Specialization == DevOpsSpecialization.Reliability)
                weight *= 1.2;
            
            if (context.Security.RequiresMfa && persona.Specialization == DevOpsSpecialization.Security)
                weight *= 1.2;
        }

        return Math.Min(1.0, weight);
    }

    private ContributionType DetermineContributionType(string personaId, List<string> involvedPersonaIds)
    {
        if (involvedPersonaIds.Count == 0)
            return ContributionType.Primary;

        if (personaId == involvedPersonaIds[0])
            return ContributionType.Primary;
        
        if (involvedPersonaIds.Count > 2 && personaId == involvedPersonaIds.Last())
            return ContributionType.Validation;
        
        return ContributionType.Supporting;
    }

    private async Task<string> ConsolidateResponsesAsync(List<PersonaContribution> contributions, DevOpsContext context)
    {
        if (!contributions.Any())
            return "No responses available.";

        if (contributions.Count == 1)
            return contributions[0].Response.Response;

        var consolidatedBuilder = new StringBuilder();
        
        // Group contributions by type
        var primaryContributions = contributions.Where(c => c.Type == ContributionType.Primary).ToList();
        var supportingContributions = contributions.Where(c => c.Type == ContributionType.Supporting).ToList();
        var validationContributions = contributions.Where(c => c.Type == ContributionType.Validation).ToList();

        // Start with primary response
        if (primaryContributions.Any())
        {
            var primary = primaryContributions.OrderByDescending(c => c.Weight).First();
            consolidatedBuilder.AppendLine(primary.Response.Response);
            consolidatedBuilder.AppendLine();
        }

        // Add supporting insights
        if (supportingContributions.Any())
        {
            consolidatedBuilder.AppendLine("### Additional Insights:");
            foreach (var support in supportingContributions.OrderByDescending(c => c.Weight))
            {
                if (_personas.TryGetValue(support.PersonaId, out var persona))
                {
                    consolidatedBuilder.AppendLine($"**{persona.Name} perspective:**");
                    consolidatedBuilder.AppendLine(ExtractKeyPoints(support.Response.Response));
                    consolidatedBuilder.AppendLine();
                }
            }
        }

        // Add validation notes
        if (validationContributions.Any())
        {
            consolidatedBuilder.AppendLine("### Validation Notes:");
            foreach (var validation in validationContributions)
            {
                if (_personas.TryGetValue(validation.PersonaId, out var persona))
                {
                    var caveats = validation.Response.Confidence.Caveats;
                    if (caveats.Any())
                    {
                        consolidatedBuilder.AppendLine($"- {persona.Name}: {string.Join("; ", caveats)}");
                    }
                }
            }
        }

        return consolidatedBuilder.ToString();
    }

    private string ExtractKeyPoints(string response)
    {
        // Simple extraction - take first few sentences or bullet points
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var keyPoints = lines.Take(3);
        return string.Join("\n", keyPoints);
    }

    private Dictionary<string, string> IdentifyConflicts(List<PersonaResponse> responses)
    {
        var conflicts = new Dictionary<string, string>();

        // Compare suggested actions
        var actionGroups = responses
            .SelectMany(r => r.SuggestedActions.Select(a => new { Response = r, Action = a }))
            .GroupBy(x => x.Action.Category)
            .Where(g => g.Count() > 1);

        foreach (var group in actionGroups)
        {
            var differentPriorities = group.Select(x => x.Action.Priority).Distinct().Count() > 1;
            if (differentPriorities)
            {
                conflicts[$"Action_{group.Key}"] = "Different priority levels suggested";
            }
        }

        // Compare confidence levels
        var confidenceRange = responses.Max(r => r.Confidence.Overall) - responses.Min(r => r.Confidence.Overall);
        if (confidenceRange > 0.3)
        {
            conflicts["Confidence"] = $"Wide confidence range: {confidenceRange:F2}";
        }

        return conflicts;
    }

    private async Task<ConflictResolution> ResolveByConsensusAsync(
        List<PersonaResponse> responses,
        Dictionary<string, string> conflicts)
    {
        var resolution = new ConflictResolution
        {
            ResolutionMethod = "Consensus"
        };
        
        foreach (var conflict in conflicts)
            resolution.ConflictingElements[conflict.Key] = conflict.Value;

        // Find common elements across all responses
        var commonSuggestions = responses
            .SelectMany(r => r.SuggestedActions)
            .GroupBy(a => a.Title)
            .Where(g => g.Count() >= responses.Count / 2) // Majority agreement
            .Select(g => g.First())
            .ToList();

        var consensusBuilder = new StringBuilder();
        consensusBuilder.AppendLine("Based on consensus from multiple perspectives:");
        consensusBuilder.AppendLine();

        foreach (var suggestion in commonSuggestions)
        {
            consensusBuilder.AppendLine($"- {suggestion.Title}: {suggestion.Description}");
        }

        resolution.ResolvedResponse = consensusBuilder.ToString();
        resolution.Confidence = commonSuggestions.Any() ? 0.8 : 0.5;

        return await Task.FromResult(resolution);
    }

    private ConflictResolution ResolveByHighestConfidence(List<PersonaResponse> responses)
    {
        var highestConfidence = responses.OrderByDescending(r => r.Confidence.Overall).First();

        return new ConflictResolution
        {
            ResolvedResponse = highestConfidence.Response,
            ResolutionMethod = "Highest Confidence",
            Confidence = highestConfidence.Confidence.Overall,
            CompromisesMade = { $"Selected response with confidence {highestConfidence.Confidence.Overall:F2}" }
        };
    }

    private ConflictResolution ResolveBySpecializationPriority(List<PersonaResponse> responses)
    {
        // This would need access to persona metadata to determine specialization
        // For now, use confidence as proxy
        return ResolveByHighestConfidence(responses);
    }

    private ConflictResolution ResolveByWeightedAverage(List<PersonaResponse> responses)
    {
        var totalWeight = responses.Sum(r => r.Confidence.Overall);
        var weightedBuilder = new StringBuilder();

        weightedBuilder.AppendLine("Weighted consensus based on confidence levels:");
        weightedBuilder.AppendLine();

        foreach (var response in responses.OrderByDescending(r => r.Confidence.Overall))
        {
            var weight = response.Confidence.Overall / totalWeight;
            if (weight > 0.2) // Only include significant contributions
            {
                weightedBuilder.AppendLine($"[Weight: {weight:F2}] {ExtractKeyPoints(response.Response)}");
                weightedBuilder.AppendLine();
            }
        }

        return new ConflictResolution
        {
            ResolvedResponse = weightedBuilder.ToString(),
            ResolutionMethod = "Weighted Average",
            Confidence = responses.Average(r => r.Confidence.Overall)
        };
    }

    private ConflictResolution ResolveByUserPreference(List<PersonaResponse> responses)
    {
        // This would need user preference data
        // For now, default to consensus
        return ResolveByHighestConfidence(responses);
    }

    private void UpdateHealthStatus(PersonaStatus status)
    {
        if (status.Health.SuccessRate > 0.9 && status.Health.ErrorCount == 0)
        {
            status.Health.Status = HealthStatus.Healthy;
        }
        else if (status.Health.SuccessRate > 0.7 || status.Health.ErrorCount < 5)
        {
            status.Health.Status = HealthStatus.Degraded;
        }
        else
        {
            status.Health.Status = HealthStatus.Unhealthy;
        }
    }

    private TaskCategory InferTaskCategory(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("deploy")) return TaskCategory.Deployment;
        if (requestLower.Contains("monitor")) return TaskCategory.Monitoring;
        if (requestLower.Contains("automat")) return TaskCategory.Automation;
        if (requestLower.Contains("security")) return TaskCategory.Security;
        if (requestLower.Contains("perform")) return TaskCategory.Performance;
        if (requestLower.Contains("plan")) return TaskCategory.Planning;

        return TaskCategory.Infrastructure;
    }

    private TaskComplexity InferComplexity(string request)
    {
        var wordCount = request.Split(' ').Length;
        
        if (wordCount < 10) return TaskComplexity.Simple;
        if (wordCount < 30) return TaskComplexity.Moderate;
        if (wordCount < 50) return TaskComplexity.Complex;
        
        return TaskComplexity.Expert;
    }

    private DevOpsSpecialization InferSpecialization(string request)
    {
        var requestLower = request.ToLowerInvariant();

        if (requestLower.Contains("security") || requestLower.Contains("vulnerab") || requestLower.Contains("compliance"))
            return DevOpsSpecialization.Security;
        
        if (requestLower.Contains("reliability") || requestLower.Contains("sre") || requestLower.Contains("incident"))
            return DevOpsSpecialization.Reliability;
        
        if (requestLower.Contains("team") || requestLower.Contains("manage") || requestLower.Contains("planning"))
            return DevOpsSpecialization.Management;
        
        if (requestLower.Contains("infrastructure") || requestLower.Contains("terraform") || requestLower.Contains("cloud"))
            return DevOpsSpecialization.Infrastructure;
        
        return DevOpsSpecialization.Development;
    }
}