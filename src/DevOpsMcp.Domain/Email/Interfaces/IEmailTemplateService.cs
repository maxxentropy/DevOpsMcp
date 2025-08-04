using DevOpsMcp.Domain.Interfaces;
using ErrorOr;

namespace DevOpsMcp.Domain.Email.Interfaces;

/// <summary>
/// Service for managing email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Create or update an email template
    /// </summary>
    Task<ErrorOr<SesEmailTemplate>> CreateTemplateAsync(
        SesEmailTemplate emailTemplate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an email template by name
    /// </summary>
    Task<ErrorOr<SesEmailTemplate>> GetTemplateAsync(
        string templateName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing template
    /// </summary>
    Task<ErrorOr<SesEmailTemplate>> UpdateTemplateAsync(
        SesEmailTemplate emailTemplate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an email template
    /// </summary>
    Task<ErrorOr<bool>> DeleteTemplateAsync(
        string templateName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List available email templates
    /// </summary>
    Task<ErrorOr<List<SesEmailTemplate>>> ListTemplatesAsync(
        int? pageSize = null,
        string? nextToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test render a template with sample data
    /// </summary>
    Task<ErrorOr<RenderedTemplate>> TestRenderTemplateAsync(
        string templateName,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken = default);
}