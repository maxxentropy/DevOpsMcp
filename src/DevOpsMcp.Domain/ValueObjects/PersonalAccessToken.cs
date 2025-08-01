namespace DevOpsMcp.Domain.ValueObjects;

public sealed record PersonalAccessToken
{
    public string Value { get; }
    
    private PersonalAccessToken(string value)
    {
        Value = value;
    }
    
    public static ErrorOr<PersonalAccessToken> Create(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("PersonalAccessToken.Empty", "Personal Access Token cannot be empty");
        }
        
        if (token.Length < 52)
        {
            return Error.Validation("PersonalAccessToken.TooShort", "Personal Access Token appears to be invalid");
        }
        
        return new PersonalAccessToken(token);
    }
    
    public string ToAuthorizationHeader()
    {
        var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($":{Value}"));
        return $"Basic {encodedToken}";
    }
    
    public override string ToString() => "***";
}