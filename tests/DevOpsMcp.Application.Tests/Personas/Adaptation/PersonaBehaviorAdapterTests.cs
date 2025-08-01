using DevOpsMcp.Application.Personas.Adaptation;
using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Adaptation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DevOpsMcp.Application.Tests.Personas.Adaptation;

public class PersonaBehaviorAdapterTests
{
    private readonly Mock<ILogger<PersonaBehaviorAdapter>> _loggerMock;
    private readonly Mock<IPersonaLearningEngine> _learningEngineMock;
    private readonly PersonaBehaviorAdapter _adapter;

    public PersonaBehaviorAdapterTests()
    {
        _loggerMock = new Mock<ILogger<PersonaBehaviorAdapter>>();
        _learningEngineMock = new Mock<IPersonaLearningEngine>();
        _adapter = new PersonaBehaviorAdapter(_loggerMock.Object, _learningEngineMock.Object);
    }

    [Fact]
    public async Task AnalyzeInteractionPatternAsync_WithPositiveFeedback_ReturnsPositiveAdjustment()
    {
        // Arrange
        var interaction = new UserInteraction
        {
            Request = "Great explanation, very helpful!",
            PersonaId = "devops-engineer",
            Type = InteractionType.Feedback,
            Duration = 30
        };
        var history = CreateInteractionHistory(5, positive: true);

        // Act
        var adjustment = await _adapter.AnalyzeInteractionPatternAsync("devops-engineer", interaction, history);

        // Assert
        adjustment.Should().NotBeNull();
        adjustment.ConfidenceScore.Should().BeGreaterThan(0.7);
        adjustment.Reasons.Should().Contain(r => r.Contains("positive"));
    }

    [Fact]
    public async Task AnalyzeInteractionPatternAsync_WithRequestForSimplification_SuggestsLowerTechnicalDepth()
    {
        // Arrange
        var interaction = new UserInteraction
        {
            Request = "Can you explain this in simpler terms?",
            PersonaId = "devops-engineer",
            Type = InteractionType.Clarification
        };
        var history = CreateInteractionHistory(3);

        // Act
        var adjustment = await _adapter.AnalyzeInteractionPatternAsync("devops-engineer", interaction, history);

        // Assert
        adjustment.SuggestedTechnicalDepth.Should().Be(TechnicalDepth.Beginner);
        adjustment.ParameterAdjustments.Should().ContainKey("technical_depth");
        adjustment.ParameterAdjustments["technical_depth"].Should().BeLessThan(0);
    }

    [Fact]
    public async Task AdaptConfigurationAsync_WithConciselPreference_UpdatesCommunicationStyle()
    {
        // Arrange
        var currentConfig = new PersonaConfiguration
        {
            CommunicationStyle = CommunicationStyle.Collaborative,
            TechnicalDepth = TechnicalDepth.Advanced,
            ResponseDetailLevel = DetailLevel.Comprehensive
        };
        var preferences = new UserPreferences
        {
            CommunicationPreference = PreferredCommunicationStyle.Concise,
            PreferredResponseLength = ResponseLength.Brief,
            PreferredTechnicalDepth = TechnicalDepth.Intermediate
        };
        var context = new ProjectContext { ProjectId = "test", CurrentPhase = "Development" };

        // Act
        var adaptedConfig = await _adapter.AdaptConfigurationAsync("devops-engineer", currentConfig, preferences, context);

        // Assert
        adaptedConfig.Should().NotBeNull();
        adaptedConfig.CommunicationStyle.Should().Be(CommunicationStyle.Concise);
        adaptedConfig.TechnicalDepth.Should().Be(TechnicalDepth.Intermediate);
    }

    [Fact]
    public async Task CalculateAdaptationConfidenceAsync_WithConsistentHistory_ReturnsHighConfidence()
    {
        // Arrange
        var history = new InteractionHistory
        {
            TotalInteractions = 50
        };
        history.RecentInteractions.AddRange(CreateRecentInteractions(10, consistent: true));
        history.Feedback.AddRange(Enumerable.Range(0, 45).Select(i => new UserFeedback { Type = FeedbackType.Positive }));
        history.Feedback.AddRange(Enumerable.Range(0, 5).Select(i => new UserFeedback { Type = FeedbackType.Negative }));

        // Act
        var confidence = await _adapter.CalculateAdaptationConfidenceAsync("devops-engineer", history);

        // Assert
        confidence.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task CalculateAdaptationConfidenceAsync_WithInconsistentHistory_ReturnsLowConfidence()
    {
        // Arrange
        var history = new InteractionHistory
        {
            TotalInteractions = 10
        };
        history.RecentInteractions.AddRange(CreateRecentInteractions(5, consistent: false));
        history.Feedback.AddRange(Enumerable.Range(0, 5).Select(i => new UserFeedback { Type = FeedbackType.Positive }));
        history.Feedback.AddRange(Enumerable.Range(0, 5).Select(i => new UserFeedback { Type = FeedbackType.Negative }));

        // Act
        var confidence = await _adapter.CalculateAdaptationConfidenceAsync("devops-engineer", history);

        // Assert
        confidence.Should().BeLessThan(0.5);
    }

    [Fact]
    public async Task LearnFromFeedbackAsync_WithPositiveFeedback_LogsSuccess()
    {
        // Arrange
        var feedback = new UserFeedback
        {
            Rating = 5,
            Type = FeedbackType.Positive,
            Comment = "Excellent response!"
        };
        var response = new PersonaResponse
        {
            ResponseId = "test-response",
            Response = "Test response content",
            Metadata = new ResponseMetadata()
        };

        // Act
        await _adapter.LearnFromFeedbackAsync("devops-engineer", feedback, response);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Learning from feedback")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("make it shorter", PreferredCommunicationStyle.Concise)]
    [InlineData("more details please", PreferredCommunicationStyle.Detailed)]
    [InlineData("step by step", PreferredCommunicationStyle.StepByStep)]
    [InlineData("just the code", PreferredCommunicationStyle.Practical)]
    public async Task AnalyzeInteractionPatternAsync_DetectsCommunicationPreferences(string request, PreferredCommunicationStyle expectedStyle)
    {
        // Arrange
        var interaction = new UserInteraction
        {
            Request = request,
            PersonaId = "devops-engineer",
            Type = InteractionType.Clarification
        };
        var history = CreateInteractionHistory(1);

        // Act
        var adjustment = await _adapter.AnalyzeInteractionPatternAsync("devops-engineer", interaction, history);

        // Assert
        adjustment.Reasons.Should().Contain(r => r.Contains("communication style"));
    }

    private InteractionHistory CreateInteractionHistory(int count, bool positive = false)
    {
        var history = new InteractionHistory
        {
            TotalInteractions = count
        };

        history.RecentInteractions.AddRange(CreateRecentInteractions(count));
        
        var positiveFeedbackCount = positive ? count - 1 : count / 2;
        var negativeFeedbackCount = positive ? 1 : count / 2;
        
        history.Feedback.AddRange(Enumerable.Range(0, positiveFeedbackCount)
            .Select(i => new UserFeedback { Type = FeedbackType.Positive }));
        history.Feedback.AddRange(Enumerable.Range(0, negativeFeedbackCount)
            .Select(i => new UserFeedback { Type = FeedbackType.Negative }));
            
        return history;
    }

    private List<UserInteraction> CreateRecentInteractions(int count, bool consistent = true)
    {
        var interactions = new List<UserInteraction>();
        for (int i = 0; i < count; i++)
        {
            interactions.Add(new UserInteraction
            {
                Request = consistent ? "Similar request" : $"Different request {i}",
                Type = consistent ? InteractionType.Query : (InteractionType)(i % 5),
                Duration = consistent ? 2.0 : i * 1.5,
                Timestamp = DateTime.UtcNow.AddMinutes(-i * 5)
            });
        }
        return interactions;
    }
}