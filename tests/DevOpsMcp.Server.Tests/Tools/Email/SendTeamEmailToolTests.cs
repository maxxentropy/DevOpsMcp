using System.Text.Json;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Server.Tools.Email;
using ErrorOr;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static DevOpsMcp.Server.Tests.Tools.Email.EmailToolTestHelpers;

namespace DevOpsMcp.Server.Tests.Tools.Email;

public class SendTeamEmailToolTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly IOptions<SesV2Options> _options;
    private readonly SendTeamEmailTool _tool;

    public SendTeamEmailToolTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        
        var options = new SesV2Options
        {
            FromAddress = "noreply@example.com"
        };
        options.TeamMembers.Add("john", "john@example.com");
        options.TeamMembers.Add("jane", "jane@example.com");
        options.TeamMembers.Add("bob", "bob@example.com");
        
        _options = Options.Create(options);
        _tool = new SendTeamEmailTool(_mockEmailService.Object, _options);
    }

    [Fact]
    public void Tool_HasCorrectMetadata()
    {
        // Assert
        Assert.Equal("send_team_email", _tool.Name);
        Assert.Equal("Send an email to all configured team members.", _tool.Description);
        Assert.NotEqual(default(JsonElement), _tool.InputSchema);
    }

    [Fact]
    public async Task ExecuteAsync_WithConfiguredTeamMembers_SendsToAll()
    {
        // Arrange
        var arguments = new SendTeamEmailToolArguments
        {
            Subject = "Team Update",
            Body = "<h1>Important Update</h1>",
            IsHtml = true
        };

        var emailResults = new List<EmailResult>
        {
            new EmailResult { Success = true, MessageId = "msg-1", RequestId = "req-1", Status = EmailStatus.Sent },
            new EmailResult { Success = true, MessageId = "msg-2", RequestId = "req-2", Status = EmailStatus.Sent },
            new EmailResult { Success = true, MessageId = "msg-3", RequestId = "req-3", Status = EmailStatus.Sent }
        };

        _mockEmailService
            .Setup(x => x.SendTeamEmailAsync(
                It.IsAny<List<string>>(),
                arguments.Subject,
                arguments.Body,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResults);

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        Assert.Equal(3, result["successCount"].GetInt32());
        Assert.Equal(3, result["totalCount"].GetInt32());
        Assert.Equal(0, result["failedCount"].GetInt32());
        
        _mockEmailService.Verify(x => x.SendTeamEmailAsync(
            It.Is<List<string>>(emails => 
                emails.Count == 3 &&
                emails.Contains("john@example.com") &&
                emails.Contains("jane@example.com") &&
                emails.Contains("bob@example.com")),
            arguments.Subject,
            arguments.Body,
            true,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoTeamMembers_ReturnsError()
    {
        // Arrange
        var emptyOptions = Options.Create(new SesV2Options
        {
            FromAddress = "noreply@example.com"
            // No team members configured
        });
        var tool = new SendTeamEmailTool(_mockEmailService.Object, emptyOptions);

        var arguments = new SendTeamEmailToolArguments
        {
            Subject = "Test",
            Body = "Test"
        };

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        Assert.Contains("No team members configured", GetResponseContent(response));
        
        _mockEmailService.Verify(x => x.SendTeamEmailAsync(
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        ), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithPartialFailures_ReportsCorrectCounts()
    {
        // Arrange
        var arguments = new SendTeamEmailToolArguments
        {
            Subject = "Test",
            Body = "Test body",
            IsHtml = false
        };

        var emailResults = new List<EmailResult>
        {
            new EmailResult { Success = true, MessageId = "msg-1", RequestId = "req-1", Status = EmailStatus.Sent },
            new EmailResult { Success = false, MessageId = null, RequestId = "req-2", Status = EmailStatus.Failed }
            // One recipient failed, so only 2 results returned
        };

        _mockEmailService
            .Setup(x => x.SendTeamEmailAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResults);

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        Assert.Equal(2, result["successCount"].GetInt32()); // Only 2 in results (successful sends)
        Assert.Equal(3, result["totalCount"].GetInt32()); // Total team members
        Assert.Equal(1, result["failedCount"].GetInt32()); // One failed
    }

    [Fact]
    public async Task ExecuteAsync_WhenServiceReturnsError_ReturnsErrorResponse()
    {
        // Arrange
        var arguments = new SendTeamEmailToolArguments
        {
            Subject = "Test",
            Body = "Test"
        };

        _mockEmailService
            .Setup(x => x.SendTeamEmailAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Service temporarily unavailable"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        var content = GetResponseContent(response);
        Assert.Contains("Failed to send team email", content);
        Assert.Contains("A failure has occurred", content);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_ReturnsErrorResponse()
    {
        // Arrange
        var arguments = new SendTeamEmailToolArguments
        {
            Subject = "Test",
            Body = "Test"
        };

        _mockEmailService
            .Setup(x => x.SendTeamEmailAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        Assert.Contains("Unexpected error", GetResponseContent(response));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullIsHtml_DefaultsToTrue()
    {
        // Arrange
        var arguments = new SendTeamEmailToolArguments
        {
            Subject = "Test",
            Body = "<p>HTML</p>",
            IsHtml = null // Not specified
        };

        _mockEmailService
            .Setup(x => x.SendTeamEmailAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailResult>());

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(x => x.SendTeamEmailAsync(
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            true, // Should default to HTML
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}