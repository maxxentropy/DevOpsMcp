using ErrorOr;

namespace DevOpsMcp.Domain.Email.Interfaces;

/// <summary>
/// Service for managing email suppression lists
/// </summary>
public interface IEmailSuppressionService
{
    /// <summary>
    /// Add an email to the suppression list
    /// </summary>
    Task<ErrorOr<SuppressionEntry>> AddToSuppressionListAsync(
        string email, 
        SuppressionReason reason,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove an email from the suppression list
    /// </summary>
    Task<ErrorOr<bool>> RemoveFromSuppressionListAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an email is on the suppression list
    /// </summary>
    Task<ErrorOr<bool>> IsEmailSuppressedAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get suppression entry details
    /// </summary>
    Task<ErrorOr<SuppressionEntry>> GetSuppressionEntryAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List suppressed emails
    /// </summary>
    Task<ErrorOr<List<SuppressionEntry>>> ListSuppressedEmailsAsync(
        int? pageSize = null,
        string? nextToken = null,
        CancellationToken cancellationToken = default);
}