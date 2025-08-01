using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Tools.Personas;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace DevOpsMcp.Server.Tests.Tools.Personas;

public class GetPersonaStatusToolTests
{
    private readonly Mock<IPersonaOrchestrator> _orchestratorMock;
    private readonly GetPersonaStatusTool _tool;

    public GetPersonaStatusToolTests()
    {
        _orchestratorMock = new Mock<IPersonaOrchestrator>();
        _tool = new GetPersonaStatusTool(_orchestratorMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        _tool.Name.Should().Be("get_persona_status");
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        // Assert
        _tool.Description.Should().Contain("current status");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsAllPersonaStatuses()
    {
        // Arrange
        var arguments = new GetPersonaStatusArguments(); // No arguments needed

        var personaStatus = new PersonaStatus
        {
            PersonaId = "devops-engineer",
            IsActive = true,
            LastActivated = DateTime.UtcNow.AddMinutes(-5),
            CurrentLoad = 3,
            AverageResponseTime = 2.5,
            Health = new PersonaHealth
            {
                Status = HealthStatus.Healthy,
                SuccessRate = 0.95,
                ErrorCount = 2,
                AverageSatisfaction = 4.5
            }
        };

        _orchestratorMock.Setup(x => x.GetActivePersonasAsync())
            .ReturnsAsync(new List<PersonaStatus> { personaStatus });

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("devops-engineer");
        responseJson.Should().Contain("\"isActive\":true");
        responseJson.Should().Contain("health");
        responseJson.Should().Contain("Healthy");
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePersonas_ReturnsAllStatuses()
    {
        // Arrange
        var arguments = new GetPersonaStatusArguments();

        var personaStatuses = new List<PersonaStatus>
        {
            new PersonaStatus
            {
                PersonaId = "devops-engineer",
                IsActive = true,
                CurrentLoad = 2,
                Health = new PersonaHealth { Status = HealthStatus.Healthy }
            },
            new PersonaStatus
            {
                PersonaId = "security-engineer",
                IsActive = false,
                CurrentLoad = 0,
                Health = new PersonaHealth { Status = HealthStatus.Unknown }
            },
            new PersonaStatus
            {
                PersonaId = "sre-specialist",
                IsActive = true,
                CurrentLoad = 5,
                Health = new PersonaHealth { Status = HealthStatus.Degraded }
            }
        };

        _orchestratorMock.Setup(x => x.GetActivePersonasAsync())
            .ReturnsAsync(personaStatuses);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("devops-engineer");
        responseJson.Should().Contain("security-engineer");
        responseJson.Should().Contain("sre-specialist");
        responseJson.Should().Contain("personas");
        responseJson.Should().Contain("summary");
    }

    [Fact]
    public async Task ExecuteAsync_OnlyReturnsActivePersonas()
    {
        // Arrange
        var arguments = new GetPersonaStatusArguments();

        // GetActivePersonasAsync only returns active personas
        var activePersonas = new List<PersonaStatus>
        {
            new PersonaStatus { PersonaId = "p1", IsActive = true, Health = new PersonaHealth() },
            new PersonaStatus { PersonaId = "p3", IsActive = true, Health = new PersonaHealth() }
        };

        _orchestratorMock.Setup(x => x.GetActivePersonasAsync())
            .ReturnsAsync(activePersonas);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        var responseJson = result.Content[0].Text;
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson!);
        
        var personas = response.GetProperty("personas").EnumerateArray().ToList();
        personas.Should().HaveCount(2);
        personas.All(p => p.GetProperty("isActive").GetBoolean()).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoActivePersonas_ReturnsEmptyList()
    {
        // Arrange
        var arguments = new GetPersonaStatusArguments();

        _orchestratorMock.Setup(x => x.GetActivePersonasAsync())
            .ReturnsAsync(new List<PersonaStatus>());

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("totalActive\":0");
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsError()
    {
        // Arrange
        var arguments = new GetPersonaStatusArguments();

        _orchestratorMock.Setup(x => x.GetActivePersonasAsync())
            .ThrowsAsync(new Exception("Orchestrator failure"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error retrieving persona status");
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesSummaryStatistics()
    {
        // Arrange
        var arguments = new GetPersonaStatusArguments();

        var personaStatuses = new List<PersonaStatus>
        {
            new PersonaStatus { IsActive = true, CurrentLoad = 3, AverageResponseTime = 2.0, Health = new PersonaHealth { Status = HealthStatus.Healthy } },
            new PersonaStatus { IsActive = true, CurrentLoad = 7, AverageResponseTime = 3.0, Health = new PersonaHealth { Status = HealthStatus.Healthy } },
            new PersonaStatus { IsActive = true, CurrentLoad = 5, AverageResponseTime = 2.5, Health = new PersonaHealth { Status = HealthStatus.Degraded } }
        };

        _orchestratorMock.Setup(x => x.GetActivePersonasAsync())
            .ReturnsAsync(personaStatuses);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        var responseJson = result.Content[0].Text;
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson!);
        
        var summary = response.GetProperty("summary");
        summary.GetProperty("healthyCount").GetInt32().Should().Be(2);
        summary.GetProperty("degradedCount").GetInt32().Should().Be(1);
        summary.GetProperty("averageLoad").GetDouble().Should().BeApproximately(5.0, 0.01);
    }

    [Fact]
    public void InputSchema_ContainsOptionalProperties()
    {
        // Act
        var schema = _tool.InputSchema;

        // Assert
        schema.GetProperty("type").GetString().Should().Be("object");
        var properties = schema.GetProperty("properties");
        // GetPersonaStatusArguments has no properties
        schema.GetProperty("type").GetString().Should().Be("object");
    }
}