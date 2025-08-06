namespace DevOpsMcp.Domain.Email;

/// <summary>
/// AWS SES sending quota information
/// </summary>
public sealed class EmailQuotaInfo
{
    public bool SendingEnabled { get; init; }
    public bool ProductionAccessEnabled { get; init; }
    public string? EnforcementStatus { get; init; }
    public string? ContactLanguage { get; init; }
    public List<string> SuppressedReasons { get; init; } = new();
    public bool? VdmEnabled { get; init; }
}