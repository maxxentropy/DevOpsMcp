using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Email;
using ErrorOr;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Service for rendering email templates
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Render a template with the provided data
    /// </summary>
    Task<ErrorOr<RenderedTemplate>> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Render a template from a template object
    /// </summary>
    Task<ErrorOr<RenderedTemplate>> RenderAsync(EmailTemplate emailTemplate, object model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate template syntax without rendering
    /// </summary>
    Task<ErrorOr<ValidationResult>> ValidateTemplateAsync(string templateContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available templates
    /// </summary>
    Task<ErrorOr<EmailTemplate[]>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific template by name
    /// </summary>
    Task<ErrorOr<EmailTemplate>> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of template rendering
/// </summary>
public sealed class RenderedTemplate
{
    /// <summary>
    /// Rendered HTML content
    /// </summary>
    public required string HtmlContent { get; init; }

    /// <summary>
    /// Plain text version (auto-generated or template-specific)
    /// </summary>
    public required string TextContent { get; init; }

    /// <summary>
    /// Inlined HTML (CSS moved inline for email clients)
    /// </summary>
    public required string InlinedHtmlContent { get; init; }

    /// <summary>
    /// Template name that was rendered
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// Rendering duration
    /// </summary>
    public required TimeSpan RenderDuration { get; init; }

    /// <summary>
    /// Whether the template was served from cache
    /// </summary>
    public bool FromCache { get; init; }
}