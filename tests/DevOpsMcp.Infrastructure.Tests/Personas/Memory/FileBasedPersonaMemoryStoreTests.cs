using DevOpsMcp.Application.Personas.Memory;
using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Infrastructure.Personas.Memory;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DevOpsMcp.Infrastructure.Tests.Personas.Memory;

public class FileBasedPersonaMemoryStoreTests : IDisposable
{
    private readonly Mock<ILogger<FileBasedPersonaMemoryStore>> _loggerMock;
    private readonly string _testBasePath;
    private readonly FileBasedPersonaMemoryStore _store;

    public FileBasedPersonaMemoryStoreTests()
    {
        _loggerMock = new Mock<ILogger<FileBasedPersonaMemoryStore>>();
        _testBasePath = Path.Combine(Path.GetTempPath(), $"PersonaMemoryTests_{Guid.NewGuid()}");
        
        var options = Options.Create(new PersonaMemoryStoreOptions
        {
            BasePath = _testBasePath,
            MaxContextsPerPersona = 5
        });
        
        _store = new FileBasedPersonaMemoryStore(_loggerMock.Object, options);
    }

    [Fact]
    public async Task SaveContextAsync_CreatesFileSuccessfully()
    {
        // Arrange
        var context = CreateTestContext("test-persona", "test-session");

        // Act
        await _store.SaveContextAsync("test-persona", context);

        // Assert
        var filePath = Path.Combine(_testBasePath, "test-persona", "context_test-session.json");
        File.Exists(filePath).Should().BeTrue();
        
        var savedContent = await File.ReadAllTextAsync(filePath);
        savedContent.Should().Contain("test-session");
        savedContent.Should().Contain("test-persona");
    }

    [Fact]
    public async Task LoadContextAsync_ReturnsStoredContext()
    {
        // Arrange
        var originalContext = CreateTestContext("test-persona", "test-session");
        await _store.SaveContextAsync("test-persona", originalContext);

        // Act
        var loadedContext = await _store.LoadContextAsync("test-persona", "test-session");

        // Assert
        loadedContext.Should().NotBeNull();
        loadedContext!.SessionId.Should().Be(originalContext.SessionId);
        loadedContext.PersonaId.Should().Be(originalContext.PersonaId);
        loadedContext.InteractionHistory.Should().HaveCount(originalContext.InteractionHistory.Count);
    }

    [Fact]
    public async Task LoadContextAsync_WithNonExistentFile_ReturnsNull()
    {
        // Act
        var result = await _store.LoadContextAsync("non-existent", "non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteContextAsync_RemovesFile()
    {
        // Arrange
        var context = CreateTestContext("test-persona", "test-session");
        await _store.SaveContextAsync("test-persona", context);
        var filePath = Path.Combine(_testBasePath, "test-persona", "context_test-session.json");
        
        // Verify file exists
        File.Exists(filePath).Should().BeTrue();

        // Act
        await _store.DeleteContextAsync("test-persona", "test-session");

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task ClearPersonaDataAsync_RemovesAllPersonaFiles()
    {
        // Arrange
        var personaId = "test-persona";
        await _store.SaveContextAsync(personaId, CreateTestContext(personaId, "session1"));
        await _store.SaveContextAsync(personaId, CreateTestContext(personaId, "session2"));
        
        var personaDir = Path.Combine(_testBasePath, personaId);
        Directory.Exists(personaDir).Should().BeTrue();

        // Act
        await _store.ClearPersonaDataAsync(personaId);

        // Assert
        Directory.Exists(personaDir).Should().BeFalse();
    }

    [Fact]
    public async Task GetSessionIdsAsync_ReturnsAllSessionIds()
    {
        // Arrange
        var personaId = "test-persona";
        await _store.SaveContextAsync(personaId, CreateTestContext(personaId, "session1"));
        await _store.SaveContextAsync(personaId, CreateTestContext(personaId, "session2"));
        await _store.SaveContextAsync(personaId, CreateTestContext(personaId, "session3"));

        // Act
        var sessionIds = await _store.GetSessionIdsAsync(personaId);

        // Assert
        sessionIds.Should().HaveCount(3);
        sessionIds.Should().Contain(new[] { "session1", "session2", "session3" });
    }

    [Fact]
    public async Task SaveLearningDataAsync_StoresAndAccumulatesLearning()
    {
        // Arrange
        var personaId = "test-persona";
        var learning1 = new PersonaLearning
        {
            PersonaId = personaId,
            Type = LearningType.UserPreference,
            Subject = "Communication",
            ConfidenceScore = 0.8
        };
        var learning2 = new PersonaLearning
        {
            PersonaId = personaId,
            Type = LearningType.BestPractice,
            Subject = "CI/CD",
            ConfidenceScore = 0.9
        };

        // Act
        await _store.SaveLearningDataAsync(personaId, learning1);
        await _store.SaveLearningDataAsync(personaId, learning2);

        // Assert
        var learningData = await _store.GetLearningDataAsync(personaId);
        learningData.Should().NotBeNull();
        learningData!.Learnings.Should().HaveCount(2);
        learningData.PatternCount.Should().Be(2);
        learningData.AdaptationConfidence.Should().BeApproximately(0.85, 0.01);
    }

    [Fact]
    public async Task GetStorageMetricsAsync_ReturnsAccurateMetrics()
    {
        // Arrange
        await _store.SaveContextAsync("persona1", CreateTestContext("persona1", "session1"));
        await _store.SaveContextAsync("persona2", CreateTestContext("persona2", "session2"));
        await Task.Delay(100); // Ensure different timestamps

        // Act
        var metrics = await _store.GetStorageMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalEntries.Should().Be(2);
        metrics.TotalSize.Should().BeGreaterThan(0);
        metrics.OldestEntry.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task CleanupOldDataAsync_RemovesExpiredFiles()
    {
        // Arrange
        var personaId = "test-persona";
        var context = CreateTestContext(personaId, "old-session");
        await _store.SaveContextAsync(personaId, context);
        
        // Modify file timestamp to be old
        var filePath = Path.Combine(_testBasePath, personaId, "context_old-session.json");
        File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow.AddDays(-10));

        // Act
        await _store.CleanupOldDataAsync(DateTime.UtcNow.AddDays(-7));

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task MaxContextsPerPersona_EnforcesLimit()
    {
        // Arrange
        var personaId = "test-persona";
        
        // Create more contexts than the limit (5)
        for (int i = 0; i < 7; i++)
        {
            await _store.SaveContextAsync(personaId, CreateTestContext(personaId, $"session{i}"));
            await Task.Delay(50); // Ensure different timestamps
        }

        // Act - Get all files
        var personaDir = Path.Combine(_testBasePath, personaId);
        var files = Directory.GetFiles(personaDir, "context_*.json");

        // Assert
        files.Length.Should().BeLessOrEqualTo(5);
        // Newest sessions should be kept
        files.Should().Contain(f => f.Contains("session6"));
        files.Should().Contain(f => f.Contains("session5"));
    }

    [Fact]
    public async Task SanitizeFileName_HandlesInvalidCharacters()
    {
        // Arrange
        var personaId = "test/persona:invalid";
        var sessionId = "session\\with*chars";
        var context = CreateTestContext(personaId, sessionId);

        // Act
        await _store.SaveContextAsync(personaId, context);

        // Assert
        var sanitizedPersonaDir = Path.Combine(_testBasePath, "test_persona_invalid");
        Directory.Exists(sanitizedPersonaDir).Should().BeTrue();
        
        var files = Directory.GetFiles(sanitizedPersonaDir, "*.json");
        files.Should().HaveCount(1);
        files[0].Should().Contain("session_with_chars");
    }

    private ConversationContext CreateTestContext(string personaId, string sessionId)
    {
        var context = new ConversationContext
        {
            PersonaId = personaId,
            SessionId = sessionId,
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            LastInteraction = DateTime.UtcNow
        };

        context.InteractionHistory.Add(new InteractionSummary
        {
            UserInput = "Test input",
            PersonaResponse = "Test response",
            Intent = "test",
            WasSuccessful = true
        });

        context.UserPreferences["test_pref"] = "value";
        context.LearningsAndInsights["test_insight"] = "insight";

        return context;
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
    }
}