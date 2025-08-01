namespace DevOpsMcp.Domain.Tests.ValueObjects;

public class PersonalAccessTokenTests
{
    [Fact]
    public void Create_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = new string('a', 52);

        // Act
        var result = PersonalAccessToken.Create(token);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Value.Should().Be(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_EmptyToken_ReturnsError(string token)
    {
        // Act
        var result = PersonalAccessToken.Create(token);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PersonalAccessToken.Empty");
    }

    [Fact]
    public void Create_NullToken_ReturnsError()
    {
        // Act
        var result = PersonalAccessToken.Create(null!);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PersonalAccessToken.Empty");
    }

    [Fact]
    public void Create_TooShortToken_ReturnsError()
    {
        // Arrange
        var token = new string('a', 51);

        // Act
        var result = PersonalAccessToken.Create(token);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PersonalAccessToken.TooShort");
    }

    [Fact]
    public void ToAuthorizationHeader_ReturnsBasicAuthHeader()
    {
        // Arrange
        var token = new string('a', 52);
        var pat = PersonalAccessToken.Create(token).Value;

        // Act
        var header = pat.ToAuthorizationHeader();

        // Assert
        header.Should().StartWith("Basic ");
        var base64Part = header.Substring(6);
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Part));
        decoded.Should().Be($":{token}");
    }

    [Fact]
    public void ToString_ReturnsMaskedValue()
    {
        // Arrange
        var token = new string('a', 52);
        var pat = PersonalAccessToken.Create(token).Value;

        // Act
        var stringValue = pat.ToString();

        // Assert
        stringValue.Should().Be("***");
    }
}