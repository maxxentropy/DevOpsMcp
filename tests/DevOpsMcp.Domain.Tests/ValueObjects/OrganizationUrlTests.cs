namespace DevOpsMcp.Domain.Tests.ValueObjects;

public sealed class OrganizationUrlTests
{
    [Theory]
    [InlineData("https://dev.azure.com/myorg")]
    [InlineData("https://myorg.visualstudio.com")]
    [InlineData("https://dev.azure.com/myorg/")]
    public void Create_ValidUrl_ReturnsSuccess(string url)
    {
        // Act
        var result = OrganizationUrl.Create(url);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Value.Should().Be(url);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_EmptyUrl_ReturnsError(string url)
    {
        // Act
        var result = OrganizationUrl.Create(url);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrganizationUrl.Empty");
    }

    [Fact]
    public void Create_NullUrl_ReturnsError()
    {
        // Act
        var result = OrganizationUrl.Create(null!);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrganizationUrl.Empty");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("http://")]
    [InlineData("ftp://dev.azure.com/myorg")]
    public void Create_InvalidUrl_ReturnsError(string url)
    {
        // Act
        var result = OrganizationUrl.Create(url);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrganizationUrl.Invalid");
    }

    [Theory]
    [InlineData("https://github.com/myorg")]
    [InlineData("https://gitlab.com/myorg")]
    [InlineData("https://example.com")]
    public void Create_NonAzureDevOpsUrl_ReturnsError(string url)
    {
        // Act
        var result = OrganizationUrl.Create(url);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrganizationUrl.InvalidHost");
    }

    [Theory]
    [InlineData("https://dev.azure.com/myorg", "myorg")]
    [InlineData("https://dev.azure.com/my-org123/", "my-org123")]
    [InlineData("https://myorg.visualstudio.com", "myorg")]
    [InlineData("https://my-org123.visualstudio.com/", "my-org123")]
    public void GetOrganizationName_ReturnsCorrectName(string url, string expectedName)
    {
        // Arrange
        var orgUrl = OrganizationUrl.Create(url).Value;

        // Act
        var name = orgUrl.GetOrganizationName();

        // Assert
        name.Should().Be(expectedName);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var url = "https://dev.azure.com/myorg";
        var orgUrl = OrganizationUrl.Create(url).Value;

        // Act
        string convertedUrl = orgUrl;

        // Assert
        convertedUrl.Should().Be(url);
    }
}