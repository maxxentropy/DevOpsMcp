using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using DevOpsMcp.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DevOpsMcp.Infrastructure.Tests.Email;

public class SesV2AccountServiceTests
{
    private readonly Mock<IAmazonSimpleEmailServiceV2> _mockSesClient;
    private readonly Mock<ILogger<SesV2AccountService>> _mockLogger;
    private readonly SesV2AccountService _accountService;

    public SesV2AccountServiceTests()
    {
        _mockSesClient = new Mock<IAmazonSimpleEmailServiceV2>();
        _mockLogger = new Mock<ILogger<SesV2AccountService>>();
        _accountService = new SesV2AccountService(_mockSesClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSendQuotaAsync_WithActiveAccount_ReturnsQuotaInfo()
    {
        // Arrange
        var accountResponse = new GetAccountResponse
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            Details = new AccountDetails { ContactLanguage = "EN" },
            SuppressionAttributes = new SuppressionAttributes
            {
                SuppressedReasons = new List<string> { "BOUNCE", "COMPLAINT" }
            },
            VdmAttributes = new VdmAttributes
            {
                VdmEnabled = "ENABLED"
            }
        };

        _mockSesClient
            .Setup(x => x.GetAccountAsync(It.IsAny<GetAccountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountResponse);

        // Act
        var result = await _accountService.GetSendQuotaAsync();

        // Assert
        Assert.True(result.IsError == false);
        Assert.True(result.Value.SendingEnabled);
        Assert.True(result.Value.ProductionAccessEnabled);
        Assert.Equal("HEALTHY", result.Value.EnforcementStatus);
        Assert.Equal("EN", result.Value.ContactLanguage);
        Assert.Equal(2, result.Value.SuppressedReasons.Count);
        Assert.True(result.Value.VdmEnabled);
    }

    [Fact]
    public async Task GetSendQuotaAsync_WhenSesThrowsException_ReturnsError()
    {
        // Arrange
        var errorMessage = "SES error occurred";
        _mockSesClient
            .Setup(x => x.GetAccountAsync(It.IsAny<GetAccountRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSimpleEmailServiceV2Exception(errorMessage));

        // Act
        var result = await _accountService.GetSendQuotaAsync();

        // Assert
        Assert.True(result.IsError);
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
    public async Task GetAccountInfoAsync_WithActiveAccount_ReturnsAccountInfo()
    {
        // Arrange
        var accountResponse = new GetAccountResponse
        {
            SendingEnabled = true,
            ProductionAccessEnabled = false,
            EnforcementStatus = "PROBATION",
            SuppressionAttributes = new SuppressionAttributes
            {
                SuppressedReasons = new List<string> { "BOUNCE" }
            }
        };

        _mockSesClient
            .Setup(x => x.GetAccountAsync(It.IsAny<GetAccountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountResponse);

        // Act
        var result = await _accountService.GetAccountInfoAsync();

        // Assert
        Assert.True(result.IsError == false);
        Assert.True(result.Value.SendingEnabled);
        Assert.False(result.Value.ProductionAccessEnabled);
        Assert.Equal("PROBATION", result.Value.EnforcementStatus);
        Assert.Single(result.Value.SuppressedReasons);
    }

    [Fact]
    public async Task GetAccountInfoAsync_WithNullSuppressionAttributes_ReturnsEmptyList()
    {
        // Arrange
        var accountResponse = new GetAccountResponse
        {
            SendingEnabled = true,
            ProductionAccessEnabled = true,
            EnforcementStatus = "HEALTHY",
            SuppressionAttributes = null
        };

        _mockSesClient
            .Setup(x => x.GetAccountAsync(It.IsAny<GetAccountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountResponse);

        // Act
        var result = await _accountService.GetAccountInfoAsync();

        // Assert
        Assert.True(result.IsError == false);
        Assert.Empty(result.Value.SuppressedReasons);
    }

    [Fact]
    public async Task GetAccountInfoAsync_WhenSesThrowsException_ReturnsError()
    {
        // Arrange
        var errorMessage = "Access denied";
        _mockSesClient
            .Setup(x => x.GetAccountAsync(It.IsAny<GetAccountRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSimpleEmailServiceV2Exception(errorMessage));

        // Act
        var result = await _accountService.GetAccountInfoAsync();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("A failure has occurred.", result.FirstError.Description);
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SesV2AccountService(null!, _mockLogger.Object));
        
        Assert.Throws<ArgumentNullException>(() => 
            new SesV2AccountService(_mockSesClient.Object, null!));
    }
}