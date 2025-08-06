using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DevOpsMcp.Infrastructure.Tests.Email;

public class SesV2EmailSenderTests
{
    private readonly Mock<IAmazonSimpleEmailServiceV2> _mockSesClient;
    private readonly Mock<ILogger<SesV2EmailSender>> _mockLogger;
    private readonly IOptions<SesV2Options> _options;
    private readonly SesV2EmailSender _emailSender;

    public SesV2EmailSenderTests()
    {
        _mockSesClient = new Mock<IAmazonSimpleEmailServiceV2>();
        _mockLogger = new Mock<ILogger<SesV2EmailSender>>();
        
        var options = new SesV2Options
        {
            FromAddress = "noreply@example.com",
            FromName = "Test Sender",
            DefaultConfigurationSet = "test-config-set"
        };
        options.ReplyToAddresses.Add("reply@example.com");
        
        _options = Options.Create(options);
        _emailSender = new SesV2EmailSender(_mockSesClient.Object, _mockLogger.Object, _options);
    }

    [Fact]
    public async Task SendEmailAsync_WithValidHtmlEmail_SendsSuccessfully()
    {
        // Arrange
        var toAddress = "recipient@example.com";
        var subject = "Test Subject";
        var body = "<h1>Test Email</h1>";
        var messageId = "test-message-id";

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse { MessageId = messageId });

        // Act
        var result = await _emailSender.SendEmailAsync(toAddress, subject, body);

        // Assert
        Assert.True(result.IsError == false);
        Assert.True(result.Value.Success);
        Assert.Equal(messageId, result.Value.MessageId);
        Assert.Equal(EmailStatus.Sent, result.Value.Status);
        
        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<SendEmailRequest>(req =>
                req.FromEmailAddress == "\"Test Sender\" <noreply@example.com>" &&
                req.Destination.ToAddresses.Contains(toAddress) &&
                req.Content.Simple.Subject.Data == subject &&
                req.Content.Simple.Body.Html.Data == body &&
                req.ConfigurationSetName == "test-config-set"
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithPlainTextEmail_SendsSuccessfully()
    {
        // Arrange
        var toAddress = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Plain text email";
        var isHtml = false;

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse { MessageId = "test-id" });

        // Act
        var result = await _emailSender.SendEmailAsync(toAddress, subject, body, isHtml);

        // Assert
        Assert.True(result.IsError == false);
        
        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<SendEmailRequest>(req =>
                req.Content.Simple.Body.Text.Data == body &&
                req.Content.Simple.Body.Html == null
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithCcAndBcc_IncludesAllRecipients()
    {
        // Arrange
        var toAddress = "to@example.com";
        var cc = new List<string> { "cc1@example.com", "cc2@example.com" };
        var bcc = new List<string> { "bcc@example.com" };

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse { MessageId = "test-id" });

        // Act
        var result = await _emailSender.SendEmailAsync(toAddress, "Subject", "Body", true, cc, bcc);

        // Assert
        Assert.True(result.IsError == false);
        
        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<SendEmailRequest>(req =>
                req.Destination.CcAddresses.Count == 2 &&
                req.Destination.CcAddresses.Contains("cc1@example.com") &&
                req.Destination.BccAddresses.Count == 1 &&
                req.Destination.BccAddresses.Contains("bcc@example.com")
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WhenSesThrowsException_ReturnsError()
    {
        // Arrange
        var errorMessage = "SES error occurred";
        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSimpleEmailServiceV2Exception(errorMessage));

        // Act
        var result = await _emailSender.SendEmailAsync("to@example.com", "Subject", "Body");

        // Assert
        Assert.True(result.IsError);
        // ErrorOr creates a generic failure message
        Assert.Equal("A failure has occurred.", result.FirstError.Description);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_WithValidTemplate_SendsSuccessfully()
    {
        // Arrange
        var toAddress = "recipient@example.com";
        var templateName = "WelcomeTemplate";
        var templateData = new Dictionary<string, object>
        {
            { "firstName", "John" },
            { "lastName", "Doe" }
        };
        var messageId = "template-message-id";

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse { MessageId = messageId });

        // Act
        var result = await _emailSender.SendTemplatedEmailAsync(toAddress, templateName, templateData);

        // Assert
        Assert.True(result.IsError == false);
        Assert.True(result.Value.Success);
        Assert.Equal(messageId, result.Value.MessageId);
        
        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<SendEmailRequest>(req =>
                req.Content.Template.TemplateName == templateName &&
                req.Content.Template.TemplateData.Contains("firstName") &&
                req.Content.Template.TemplateData.Contains("John")
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendTeamEmailAsync_WithMultipleRecipients_SendsToAll()
    {
        // Arrange
        var teamEmails = new List<string> { "member1@example.com", "member2@example.com", "member3@example.com" };
        var subject = "Team Update";
        var body = "Important team message";

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse { MessageId = Guid.NewGuid().ToString() });

        // Act
        var result = await _emailSender.SendTeamEmailAsync(teamEmails, subject, body);

        // Assert
        Assert.True(result.IsError == false);
        Assert.Equal(3, result.Value.Count);
        Assert.All(result.Value, r => Assert.True(r.Success));
        
        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.IsAny<SendEmailRequest>(),
            It.IsAny<CancellationToken>()
        ), Times.Exactly(3));
    }

    [Fact]
    public async Task SendTeamEmailAsync_WithSomeFailures_ReturnsPartialSuccess()
    {
        // Arrange
        var teamEmails = new List<string> { "success@example.com", "fail@example.com", "success2@example.com" };
        var callCount = 0;

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2) // Second email fails
                {
                    throw new AmazonSimpleEmailServiceV2Exception("Failed to send");
                }
                return new SendEmailResponse { MessageId = $"message-{callCount}" };
            });

        // Act
        var result = await _emailSender.SendTeamEmailAsync(teamEmails, "Subject", "Body");

        // Assert
        Assert.True(result.IsError == false);
        Assert.Equal(2, result.Value.Count); // Only successful sends
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendTeamEmailAsync_WithAllFailures_ReturnsError()
    {
        // Arrange
        var teamEmails = new List<string> { "fail1@example.com", "fail2@example.com" };

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSimpleEmailServiceV2Exception("All failed"));

        // Act
        var result = await _emailSender.SendTeamEmailAsync(teamEmails, "Subject", "Body");

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SesV2EmailSender(null!, _mockLogger.Object, _options));
        
        Assert.Throws<ArgumentNullException>(() => 
            new SesV2EmailSender(_mockSesClient.Object, null!, _options));
        
        Assert.Throws<ArgumentNullException>(() => 
            new SesV2EmailSender(_mockSesClient.Object, _mockLogger.Object, null!));
    }

    [Fact]
    public async Task SendEmailAsync_WithoutFromName_UsesEmailOnly()
    {
        // Arrange
        var options = new SesV2Options
        {
            FromAddress = "noreply@example.com",
            FromName = null // No display name
        };
        var sender = new SesV2EmailSender(_mockSesClient.Object, _mockLogger.Object, Options.Create(options));

        _mockSesClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse { MessageId = "test-id" });

        // Act
        await sender.SendEmailAsync("to@example.com", "Subject", "Body");

        // Assert
        _mockSesClient.Verify(x => x.SendEmailAsync(
            It.Is<SendEmailRequest>(req =>
                req.FromEmailAddress == "noreply@example.com" // No display name formatting
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}