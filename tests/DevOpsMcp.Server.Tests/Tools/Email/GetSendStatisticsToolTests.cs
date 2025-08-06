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

public class GetSendStatisticsToolTests
{
    private readonly Mock<IEmailAccountService> _mockAccountService;
    private readonly GetSendStatisticsTool _tool;

    public GetSendStatisticsToolTests()
    {
        _mockAccountService = new Mock<IEmailAccountService>();
        _tool = new GetSendStatisticsTool(_mockAccountService.Object);
    }

    [Fact]
    public void Tool_HasCorrectMetadata()
    {
        // Assert
        Assert.Equal("get_send_statistics", _tool.Name);
        Assert.Equal("Get AWS SES account status and configuration information.", _tool.Description);
        Assert.NotEqual(default(JsonElement), _tool.InputSchema);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveAccount_ReturnsAccountInfo()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        var accountInfo = new EmailAccountInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            SuppressedReasons = new List<string> { "BOUNCE", "COMPLAINT" }
        };

        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(accountInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsDictionary(response);
        Assert.NotNull(result);
        Assert.True(result["success"].GetBoolean());
        
        var account = result["account"];
        Assert.True(account.GetProperty("sendingEnabled").GetBoolean());
        Assert.True(account.GetProperty("productionAccess").GetBoolean());
        Assert.Equal("HEALTHY", account.GetProperty("enforcementStatus").GetString());
        
        Assert.Contains("For detailed sending statistics", result["note"].GetString());
        
        _mockAccountService.Verify(x => x.GetAccountInfoAsync(
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSandboxAccount_ShowsSandboxStatus()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        var accountInfo = new EmailAccountInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = false, // Sandbox mode
            EnforcementStatus = "PROBATION",
            SuppressedReasons = new List<string>()
        };

        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(accountInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        var account = result.GetProperty("account");
        
        Assert.True(account.GetProperty("sendingEnabled").GetBoolean());
        Assert.False(account.GetProperty("productionAccess").GetBoolean()); // In sandbox
        Assert.Equal("PROBATION", account.GetProperty("enforcementStatus").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithSuppressionAttributes_IncludesSuppressedReasons()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        var accountInfo = new EmailAccountInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            SuppressedReasons = new List<string> { "BOUNCE", "COMPLAINT", "MANUAL" }
        };

        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(accountInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        var account = result.GetProperty("account");
        var suppressionAttrs = account.GetProperty("suppressionAttributes");
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

    [Fact]
    public async Task ExecuteAsync_WithNoSuppressionAttributes_HandlesNull()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        var accountInfo = new EmailAccountInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            SuppressedReasons = new List<string>() // Empty list
        };

        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(accountInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        var account = result.GetProperty("account");
        
        Assert.Equal(JsonValueKind.Null, account.GetProperty("suppressionAttributes").ValueKind);
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledAccount_ShowsDisabledStatus()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        var accountInfo = new EmailAccountInfo
        {
            SendingEnabled = false, // Sending disabled
            ProductionAccessEnabled = false,
            EnforcementStatus = "SHUTDOWN",
            SuppressedReasons = new List<string>()
        };

        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(accountInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        var account = result.GetProperty("account");
        
        Assert.False(account.GetProperty("sendingEnabled").GetBoolean()); // Disabled
        Assert.False(account.GetProperty("productionAccess").GetBoolean());
        Assert.Equal("SHUTDOWN", account.GetProperty("enforcementStatus").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenApiThrowsException_ReturnsError()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Access denied"));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.True(response.IsError);
        var content = GetResponseContent(response);
        Assert.Contains("Failed to get account information", content);
        Assert.Contains("A failure has occurred", content);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownEnforcementStatus_HandlesNull()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        var accountInfo = new EmailAccountInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = null, // Unknown status
            SuppressedReasons = new List<string>()
        };

        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(accountInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        var account = result.GetProperty("account");
        
        Assert.Equal("Unknown", account.GetProperty("enforcementStatus").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_AlwaysIncludesCloudWatchNote()
    {
        // Arrange
        var arguments = new GetSendStatisticsToolArguments();
        
        var accountInfo = new EmailAccountInfo
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            SuppressedReasons = new List<string>()
        };

        _mockAccountService
            .Setup(x => x.GetAccountInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOrFactory.From(accountInfo));

        // Act
        var jsonArgs = JsonSerializer.SerializeToElement(arguments);
        var response = await _tool.ExecuteAsync(jsonArgs, CancellationToken.None);

        // Assert
        Assert.False(response.IsError);
        
        var result = DeserializeResponseAsJsonElement(response);
        var note = result.GetProperty("note").GetString();
        
        Assert.NotNull(note);
        Assert.Contains("AWS CloudWatch", note);
        Assert.Contains("AWS Console", note);
        Assert.Contains("AWS SES V2 API provides metrics through CloudWatch", note);
    }
}