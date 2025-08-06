namespace DevOpsMcp.Domain.Email;

/// <summary>
/// AWS SES account information
/// </summary>
public sealed class EmailAccountInfo
{
    public bool SendingEnabled { get; init; }
    public bool ProductionAccessEnabled { get; init; }
    public string? EnforcementStatus { get; init; }
    public List<string> SuppressedReasons { get; init; } = new();
}