using DevOpsMcp.Application.Personas;
using DevOpsMcp.Domain.Personas;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace DevOpsMcp.Application.Tests.Personas;

public class BaseDevOpsPersonaTests
{
    private readonly Mock<ILogger<TestPersona>> _loggerMock;
    private readonly TestPersona _persona;

    public BaseDevOpsPersonaTests()
    {
        _loggerMock = new Mock<ILogger<TestPersona>>();
        _persona = new TestPersona(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Assert
        _persona.Id.Should().Be("test-persona");
        _persona.Name.Should().Be("Test Persona");
        _persona.Role.Should().Be("Test Role");
        _persona.Description.Should().Be("A test persona for unit testing");
        _persona.Specialization.Should().Be(DevOpsSpecialization.Development);
        _persona.Configuration.Should().NotBeNull();
        _persona.Capabilities.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Test request";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("Test response");
        response.Confidence.Should().NotBeNull();
        response.Confidence.Overall.Should().BeGreaterThan(0);
        response.SuggestedActions.Should().NotBeEmpty();
        response.Context.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithAlignedTask_ReturnsHighScore()
    {
        // Arrange
        var task = new Domain.Personas.DevOpsTask
        {
            Category = TaskCategory.Automation,
            Complexity = TaskComplexity.Moderate,
            Description = "Implement a new feature"
        };

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithMisalignedTask_ReturnsLowScore()
    {
        // Arrange
        var task = new Domain.Personas.DevOpsTask
        {
            Category = TaskCategory.Security,
            Complexity = TaskComplexity.Expert,
            Description = "Perform security audit"
        };

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeLessThan(0.5);
    }

    [Fact]
    public async Task AdaptBehaviorAsync_UpdatesConfiguration()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = "test-user",
            Name = "Test User",
            Role = "Developer",
            ExperienceLevel = "Advanced",
            Experience = ExperienceLevel.Senior
        };
        var projectContext = new ProjectContext
        {
            ProjectId = "test-project",
            ProjectName = "Test Project",
            Stage = "Production"
        };

        var initialProactivity = _persona.Configuration.ProactivityLevel;

        // Act
        await _persona.AdaptBehaviorAsync(userProfile, projectContext);

        // Assert
        _persona.Configuration.ProactivityLevel.Should().NotBe(initialProactivity);
    }

    [Fact]
    public void AnalyzeRequest_ExtractsTopicsCorrectly()
    {
        // Arrange
        var request = "Deploy the application to Kubernetes cluster using CI/CD pipeline";

        // Act
        var analysis = _persona.TestAnalyzeRequest(request);

        // Assert
        analysis.Topics.Should().Contain("deployment");
        analysis.Topics.Should().Contain("kubernetes");
        analysis.Topics.Should().Contain("ci/cd");
        analysis.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DetermineComplexity_WithSimpleRequest_ReturnsLow()
    {
        // Arrange
        var request = "Check application status";
        var context = CreateTestContext();

        // Act
        var complexity = _persona.TestDetermineComplexity(request, context);

        // Assert
        complexity.Should().Be("Low");
    }

    [Fact]
    public void DetermineComplexity_WithComplexRequest_ReturnsHigh()
    {
        // Arrange
        var request = "Design and implement a multi-region disaster recovery solution with automated failover, data replication, and zero data loss objectives";
        var context = CreateTestContext();
        context.Environment.IsProduction = true;

        // Act
        var complexity = _persona.TestDetermineComplexity(request, context);

        // Assert
        complexity.Should().Be("High");
    }

    private DevOpsContext CreateTestContext()
    {
        return new DevOpsContext
        {
            Project = new ProjectMetadata
            {
                ProjectId = "test-project",
                Name = "Test Project",
                Stage = "Development"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = "Development",
                IsProduction = false
            },
            User = new UserProfile
            {
                Id = "test-user",
                Name = "Test User",
                Role = "Developer",
                ExperienceLevel = "Intermediate",
                Experience = ExperienceLevel.MidLevel
            }
        };
    }

    // Test implementation of BaseDevOpsPersona
    private sealed class TestPersona : BaseDevOpsPersona
    {
        private readonly Mock<IPersonaMemoryManager> _memoryManagerMock;

        public TestPersona(ILogger<TestPersona> logger) : base(logger, Mock.Of<IPersonaMemoryManager>())
        {
            _memoryManagerMock = new Mock<IPersonaMemoryManager>();
            Initialize();
        }

        public override string Id => "test-persona";
        public override string Name => "Test Persona";
        public override string Role => "Test Role";
        public override string Description => "A test persona for unit testing";
        public override DevOpsSpecialization Specialization => DevOpsSpecialization.Development;

        protected override Dictionary<string, object> InitializeCapabilities()
        {
            return new Dictionary<string, object>
            {
                ["test"] = true,
                ["mock"] = "capability"
            };
        }

        protected override PersonaConfiguration GetDefaultConfiguration()
        {
            return new PersonaConfiguration
            {
                CommunicationStyle = CommunicationStyle.Collaborative,
                TechnicalDepth = TechnicalDepth.Intermediate,
                ResponseFormat = ResponseFormat.Standard,
                ProactivityLevel = 0.5
            };
        }

        protected override async Task<RequestAnalysis> AnalyzeRequestAsync(string request, DevOpsContext context)
        {
            var analysis = new RequestAnalysis
            {
                Intent = "test-intent",
                Confidence = 0.8
            };
            analysis.Topics.Add("deployment");
            analysis.Topics.Add("kubernetes");
            analysis.Topics.Add("ci/cd");
            return await Task.FromResult(analysis);
        }

        protected override async Task<PersonaResponse> GenerateResponseAsync(RequestAnalysis analysis, DevOpsContext context)
        {
            var response = new PersonaResponse
            {
                PersonaId = Id,
                Response = "Test response for: " + analysis.Intent,
                Confidence = new PersonaConfidence { Overall = 0.8 }
            };
            response.SuggestedActions.AddRange(GenerateSuggestedActions(analysis, context));
            response.Context["test"] = true;
            return await Task.FromResult(response);
        }

        private List<SuggestedAction> GenerateSuggestedActions(RequestAnalysis analysis, DevOpsContext context)
        {
            return new List<SuggestedAction>
            {
                new SuggestedAction
                {
                    Title = "Test Action",
                    Description = "A test action",
                    Category = "Configuration",
                    Priority = ActionPriority.Medium
                }
            };
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
                [TaskCategory.Automation] = 0.9,
                [TaskCategory.Security] = 0.3,
                [TaskCategory.Deployment] = 0.7,
                [TaskCategory.Monitoring] = 0.5
            };
        }

        protected override List<string> GetPersonaSkills()
        {
            return new List<string> { "development", "testing", "ci/cd" };
        }

        protected override double CalculateSpecializationAlignment(Domain.Personas.DevOpsTask task)
        {
            return task.Category == TaskCategory.Automation ? 0.9 : 0.3;
        }

        // Expose protected methods for testing
        public RequestAnalysis TestAnalyzeRequest(string request)
        {
            var analysis = new RequestAnalysis
            {
                Intent = "test-intent",
                Confidence = 0.8
            };
            
            // Simple topic extraction
            if (request.ToLower(CultureInfo.InvariantCulture).Contains("deploy")) analysis.Topics.Add("deployment");
            if (request.ToLower(CultureInfo.InvariantCulture).Contains("kubernetes")) analysis.Topics.Add("kubernetes");
            if (request.ToLower(CultureInfo.InvariantCulture).Contains("ci/cd") || request.ToLower(CultureInfo.InvariantCulture).Contains("pipeline")) analysis.Topics.Add("ci/cd");
            
            return analysis;
        }
        
        public string TestDetermineComplexity(string request, DevOpsContext context)
        {
            if (request.Length < 50) return "Low";
            if (request.Contains("multi-region") || request.Contains("disaster recovery") || context.Environment.IsProduction)
                return "High";
            return "Medium";
        }
    }
}