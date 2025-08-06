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

public class GetSendQuotaToolTests
{
    private readonly Mock<IEmailAccountService> _mockAccountService;
    private readonly GetSendQuotaTool _tool;

    public GetSendQuotaToolTests()
    {
        _mockAccountService = new Mock<IEmailAccountService>();
        _tool = new GetSendQuotaTool(_mockAccountService.Object);
    }

    [Fact]
    public void Tool_HasCorrectMetadata()
    {
        // Assert
        Assert.Equal("get_send_quota", _tool.Name);
        Assert.Equal("Get the current AWS SES account sending quota and usage.", _tool.Description);
        Assert.NotEqual(default(JsonElement), _tool.InputSchema);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveAccount_ReturnsQuotaInfo()
    {
        // Arrange
        var arguments = new GetSendQuotaToolArguments();
        
        var quotaInfo = new EmailQuotaInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            ContactLanguage = "EN",
            SuppressedReasons = new List<string> { "BOUNCE", "COMPLAINT" },
            VdmEnabled = true
        };

        _mockAccountService
            .Setup(x => x.GetSendQuotaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(quotaInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        Assert.True(result["sendingEnabled"].GetBoolean());
        Assert.True(result["productionAccessEnabled"].GetBoolean());
        Assert.Equal("HEALTHY", result["enforcementStatus"].GetString());
        
        _mockAccountService.Verify(x => x.GetSendQuotaAsync(
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSandboxAccount_ReturnsLimitedAccess()
    {
        // Arrange
        var arguments = new GetSendQuotaToolArguments();
        
        var quotaInfo = new EmailQuotaInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = false, // Sandbox mode
            EnforcementStatus = "PROBATION",
            SuppressedReasons = new List<string>()
        };

        _mockAccountService
            .Setup(x => x.GetSendQuotaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(quotaInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        Assert.True(result["sendingEnabled"].GetBoolean());
        Assert.False(result["productionAccessEnabled"].GetBoolean()); // In sandbox
        Assert.Equal("PROBATION", result["enforcementStatus"].GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledSending_ReturnsSendingDisabled()
    {
        // Arrange
        var arguments = new GetSendQuotaToolArguments();
        
        var quotaInfo = new EmailQuotaInfo
        {
            SendingEnabled = false, // Sending disabled
            ProductionAccessEnabled = false,
            EnforcementStatus = "SHUTDOWN",
            SuppressedReasons = new List<string>()
        };

        _mockAccountService
            .Setup(x => x.GetSendQuotaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(quotaInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        Assert.False(result["sendingEnabled"].GetBoolean()); // Sending disabled
        Assert.Equal("SHUTDOWN", result["enforcementStatus"].GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithMinimalResponse_HandlesNullValues()
    {
        // Arrange
        var arguments = new GetSendQuotaToolArguments();
        
        var quotaInfo = new EmailQuotaInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = null,
            ContactLanguage = null,
            SuppressedReasons = new List<string>(),
            VdmEnabled = null
        };

        _mockAccountService
            .Setup(x => x.GetSendQuotaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(quotaInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("sendingEnabled").GetBoolean());
        Assert.True(result.GetProperty("productionAccessEnabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("enforcementStatus").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("details").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("vdmAttributes").ValueKind);
    }

    [Fact]
    public async Task ExecuteAsync_WhenApiThrowsException_ReturnsError()
    {
        // Arrange
        var arguments = new GetSendQuotaToolArguments();
        
        _mockAccountService
            .Setup(x => x.GetSendQuotaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Access denied"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        var content = GetResponseContent(response);
        Assert.Contains("Failed to get send quota", content);
        Assert.Contains("A failure has occurred", content);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuppressionAttributes_IncludesSuppressedReasons()
    {
        // Arrange
        var arguments = new GetSendQuotaToolArguments();
        var suppressedReasons = new List<string> { "BOUNCE", "COMPLAINT", "MANUAL" };
        
        var quotaInfo = new EmailQuotaInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            SuppressedReasons = suppressedReasons
        };

        _mockAccountService
            .Setup(x => x.GetSendQuotaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(quotaInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        var suppressionAttrs = result.GetProperty("suppressionAttributes");
        var reasons = suppressionAttrs.GetProperty("suppressedReasons");
        
        Assert.Equal(3, reasons.GetArrayLength());
        var reasonsList = new List<string>();
        foreach (var reason in reasons.EnumerateArray())
        {
            reasonsList.Add(reason.GetString()!);
        }
        Assert.Contains("BOUNCE", reasonsList);
        Assert.Contains("COMPLAINT", reasonsList);
        Assert.Contains("MANUAL", reasonsList);
    }
}