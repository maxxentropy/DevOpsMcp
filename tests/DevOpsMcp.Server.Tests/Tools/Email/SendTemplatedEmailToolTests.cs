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

public class SendTemplatedEmailToolTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly SendTemplatedEmailTool _tool;

    public SendTemplatedEmailToolTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _tool = new SendTemplatedEmailTool(_mockEmailService.Object);
    }

    [Fact]
    public void Tool_HasCorrectMetadata()
    {
        // Assert
        Assert.Equal("send_templated_email", _tool.Name);
        Assert.Equal("Send an email using an AWS SES template with variable substitution.", _tool.Description);
        Assert.NotEqual(default(JsonElement), _tool.InputSchema);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTemplate_SendsSuccessfully()
    {
        // Arrange
        var arguments = new SendTemplatedEmailToolArguments
        {
            To = "recipient@example.com",
            TemplateName = "WelcomeEmail",
            TemplateData = new Dictionary<string, object>
            {
                { "firstName", "John" },
                { "lastName", "Doe" },
                { "accountId", "12345" }
            },
            Cc = new List<string> { "cc@example.com" }
        };

        var emailResult = new EmailResult
        {
            Success = true,
            MessageId = "template-msg-123",
            RequestId = "req-123",
            Status = EmailStatus.Sent,
            Timestamp = DateTime.UtcNow
        };

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                arguments.To,
                arguments.TemplateName,
                It.IsAny<Dictionary<string, object>>(),
                arguments.Cc,
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResult);

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        if (response.IsError == true)
        {
            var errorContent = GetResponseContent(response);
            Assert.Fail($"Unexpected error: {errorContent}");
        }
        Assert.False(response.IsError ?? false);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        Assert.Equal("template-msg-123", result["messageId"].GetString());
        Assert.Equal("recipient@example.com", result["to"].GetString());
        Assert.Equal("WelcomeEmail", result["templateName"].GetString());
        
        _mockEmailService.Verify(x => x.SendTemplatedEmailAsync(
            arguments.To,
            arguments.TemplateName,
            It.Is<Dictionary<string, object>>(data => 
                data.Count == 3 &&
                data.ContainsKey("firstName") &&
                data.ContainsKey("lastName") &&
                data.ContainsKey("accountId")),
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexTemplateData_HandlesNestedObjects()
    {
        // Arrange
        var arguments = new SendTemplatedEmailToolArguments
        {
            To = "user@example.com",
            TemplateName = "OrderConfirmation",
            TemplateData = new Dictionary<string, object>
            {
                { "customerName", "Jane Smith" },
                { "orderNumber", "ORD-12345" },
                { "items", new List<object>
                    {
                        new { name = "Widget", price = 9.99, quantity = 2 },
                        new { name = "Gadget", price = 19.99, quantity = 1 }
                    }
                },
                { "shippingAddress", new
                    {
                        street = "123 Main St",
                        city = "Springfield",
                        state = "IL",
                        zip = "62701"
                    }
                }
            }
        };

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult
            {
                Success = true,
                MessageId = "order-msg-123",
                RequestId = "req-123",
                Status = EmailStatus.Sent
            });

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        _mockEmailService.Verify(x => x.SendTemplatedEmailAsync(
            arguments.To,
            arguments.TemplateName,
            It.Is<Dictionary<string, object>>(data => 
                data.ContainsKey("customerName") &&
                data.ContainsKey("orderNumber") &&
                data.ContainsKey("items") &&
                data.ContainsKey("shippingAddress")),
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyTemplateData_SendsWithEmptyData()
    {
        // Arrange
        var arguments = new SendTemplatedEmailToolArguments
        {
            To = "recipient@example.com",
            TemplateName = "SimpleNotification",
            TemplateData = new Dictionary<string, object>() // Empty data
        };

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult
            {
                Success = true,
                MessageId = "msg-456",
                RequestId = "req-456",
                Status = EmailStatus.Sent
            });

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        _mockEmailService.Verify(x => x.SendTemplatedEmailAsync(
            arguments.To,
            arguments.TemplateName,
            It.Is<Dictionary<string, object>>(data => data.Count == 0),
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullTemplateData_SendsWithEmptyDictionary()
    {
        // Arrange
        var arguments = new SendTemplatedEmailToolArguments
        {
            To = "recipient@example.com",
            TemplateName = "BasicTemplate",
            TemplateData = null! // Null data should be treated as empty
        };

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult
            {
                Success = true,
                MessageId = "msg-789",
                RequestId = "req-789",
                Status = EmailStatus.Sent
            });

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        _mockEmailService.Verify(x => x.SendTemplatedEmailAsync(
            arguments.To,
            arguments.TemplateName,
            It.Is<Dictionary<string, object>>(data => data != null && data.Count == 0),
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTemplateNotFound_ReturnsError()
    {
        // Arrange
        var arguments = new SendTemplatedEmailToolArguments
        {
            To = "recipient@example.com",
            TemplateName = "NonExistentTemplate",
            TemplateData = new Dictionary<string, object>()
        };

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Template 'NonExistentTemplate' not found"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        var content = GetResponseContent(response);
        Assert.Contains("Failed to send templated email", content);
        Assert.Contains("A failure has occurred", content);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_ReturnsErrorResponse()
    {
        // Arrange
        var arguments = new SendTemplatedEmailToolArguments
        {
            To = "test@example.com",
            TemplateName = "TestTemplate",
            TemplateData = new Dictionary<string, object> { { "key", "value" } }
        };

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        var content = GetResponseContent(response);
        Assert.Contains("Unexpected error", content);
        Assert.Contains("Service error", content);
    }

    [Fact]
    public async Task ExecuteAsync_WithBccRecipients_IncludesBcc()
    {
        // Arrange
        var arguments = new SendTemplatedEmailToolArguments
        {
            To = "recipient@example.com",
            TemplateName = "Newsletter",
            TemplateData = new Dictionary<string, object> { { "month", "January" } },
            Bcc = new List<string> { "archive@example.com", "monitoring@example.com" }
        };

        _mockEmailService
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult
            {
                Success = true,
                MessageId = "msg-bcc",
                RequestId = "req-bcc",
                Status = EmailStatus.Sent
            });

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        _mockEmailService.Verify(x => x.SendTemplatedEmailAsync(
            arguments.To,
            arguments.TemplateName,
            It.IsAny<Dictionary<string, object>>(),
            null, // No CC
            It.Is<List<string>>(bcc => 
                bcc != null && 
                bcc.Count == 2 && 
                bcc.Contains("archive@example.com") &&
                bcc.Contains("monitoring@example.com")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}