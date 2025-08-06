using System.Text.Json;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Server.Tools.Email;
using ErrorOr;
using Moq;
using Xunit;
using static DevOpsMcp.Server.Tests.Tools.Email.EmailToolTestHelpers;

namespace DevOpsMcp.Server.Tests.Tools.Email;

public class SendEmailToolTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly SendEmailTool _tool;

    public SendEmailToolTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _tool = new SendEmailTool(_mockEmailService.Object);
    }

    [Fact]
    public void Tool_HasCorrectMetadata()
    {
        // Assert
        Assert.Equal("send_email", _tool.Name);
        Assert.Equal("Send an email using AWS SES. Supports both HTML and plain text content.", _tool.Description);
        Assert.NotEqual(default(JsonElement), _tool.InputSchema);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidHtmlEmail_ReturnsSuccess()
    {
        // Arrange
        var arguments = new SendEmailToolArguments
        {
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "<h1>Test</h1>",
            IsHtml = true,
            Cc = new List<string> { "cc@example.com" },
            Bcc = new List<string> { "bcc@example.com" }
        };

        var emailResult = new EmailResult
        {
            Success = true,
            MessageId = "msg-123",
            RequestId = "req-123",
            Status = EmailStatus.Sent,
            Timestamp = DateTime.UtcNow
        };

        _mockEmailService
            .Setup(x => x.SendEmailAsync(
                arguments.To,
                arguments.Subject,
                arguments.Body,
                true,
                arguments.Cc,
                arguments.Bcc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResult);

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        Assert.Equal("msg-123", result["messageId"].GetString());
        Assert.Equal("recipient@example.com", result["to"].GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithPlainTextEmail_SendsAsPlainText()
    {
        // Arrange
        var arguments = new SendEmailToolArguments
        {
            To = "recipient@example.com",
            Subject = "Plain Text",
            Body = "This is plain text",
            IsHtml = false
        };

        _mockEmailService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                false, // Should be plain text
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult
            {
                Success = true,
                MessageId = "msg-456",
                RequestId = "req-456",
                Status = EmailStatus.Sent,
                Timestamp = DateTime.UtcNow
            });

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        _mockEmailService.Verify(x => x.SendEmailAsync(
            arguments.To,
            arguments.Subject,
            arguments.Body,
            false, // Verify plain text
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailServiceReturnsError_ReturnsErrorResponse()
    {
        // Arrange
        var arguments = new SendEmailToolArguments
        {
            To = "invalid@example.com",
            Subject = "Test",
            Body = "Test"
        };

        _mockEmailService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Email rejected: Invalid recipient"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        var content = GetResponseContent(response);
        Assert.Contains("Failed to send email", content);
        Assert.Contains("A failure has occurred", content);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_ReturnsErrorResponse()
    {
        // Arrange
        var arguments = new SendEmailToolArguments
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test"
        };

        _mockEmailService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        var content = GetResponseContent(response);
        Assert.Contains("Unexpected error", content);
        Assert.Contains("Service unavailable", content);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullIsHtml_DefaultsToTrue()
    {
        // Arrange
        var arguments = new SendEmailToolArguments
        {
            To = "recipient@example.com",
            Subject = "Test",
            Body = "<p>HTML content</p>",
            IsHtml = null // Not specified
        };

        _mockEmailService
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult
            {
                Success = true,
                MessageId = "msg-789",
                RequestId = "req-789",
                Status = EmailStatus.Sent,
                Timestamp = DateTime.UtcNow
            });

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            true, // Should default to HTML
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}