using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using ErrorOr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreMailer.Net;
using RazorLight;

namespace DevOpsMcp.Infrastructure.Email;

/// <summary>
/// Razor-based email template renderer
/// </summary>
public sealed class RazorEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IRazorLightEngine _razorEngine;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RazorEmailTemplateRenderer> _logger;
    private readonly EmailOptions _options;
    private readonly string _templatesPath;

    public RazorEmailTemplateRenderer(
        IMemoryCache cache,
        ILogger<RazorEmailTemplateRenderer> logger,
        IOptions<EmailOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        
        _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _options.TemplatesPath);
        EnsureTemplatesDirectory();

        _razorEngine = new RazorLightEngineBuilder()
            .UseFileSystemProject(_templatesPath)
            .UseMemoryCachingProvider()
            .EnableDebugMode()
            .Build();
    }

    public async Task<ErrorOr<RenderedTemplate>> RenderAsync(
        string templateName, 
        object model, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var cacheKey = $"email_template_{templateName}_{model.GetHashCode()}";

        try
        {
            // Check cache first
            if (_cache.TryGetValue<RenderedTemplate>(cacheKey, out var cached) && cached != null)
            {
                _logger.LogDebug("Serving cached template {TemplateName}", templateName);
                return new RenderedTemplate
                {
                    HtmlContent = cached.HtmlContent,
                    TextContent = cached.TextContent,
                    InlinedHtmlContent = cached.InlinedHtmlContent,
                    TemplateName = cached.TemplateName,
                    RenderDuration = cached.RenderDuration,
                    FromCache = true
                };
            }

            // Load template
            var templateResult = await GetTemplateAsync(templateName, cancellationToken);
            if (templateResult.IsError)
            {
                return templateResult.Errors;
            }

            var template = templateResult.Value;
            return await RenderInternalAsync(template, model, cacheKey, startTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", templateName);
            return Error.Failure($"Template rendering failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<RenderedTemplate>> RenderAsync(
        EmailTemplate emailTemplate, 
        object model, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var cacheKey = $"email_template_content_{emailTemplate.Name}_{model.GetHashCode()}";

        try
        {
            // Check cache first
            if (_cache.TryGetValue<RenderedTemplate>(cacheKey, out var cached) && cached != null)
            {
                _logger.LogDebug("Serving cached template {TemplateName}", emailTemplate.Name);
                return new RenderedTemplate
                {
                    HtmlContent = cached.HtmlContent,
                    TextContent = cached.TextContent,
                    InlinedHtmlContent = cached.InlinedHtmlContent,
                    TemplateName = cached.TemplateName,
                    RenderDuration = cached.RenderDuration,
                    FromCache = true
                };
            }

            return await RenderInternalAsync(emailTemplate, model, cacheKey, startTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", emailTemplate.Name);
            return Error.Failure($"Template rendering failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<DevOpsMcp.Domain.Interfaces.ValidationResult>> ValidateTemplateAsync(
        string templateContent, 
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        try
        {
            // Try to compile the template
            var key = $"validation_{Guid.NewGuid()}";
            await _razorEngine.CompileRenderStringAsync(key, templateContent, new { });
            
            return new DevOpsMcp.Domain.Interfaces.ValidationResult
            {
                IsValid = true,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Template compilation failed: {ex.Message}");
            return new DevOpsMcp.Domain.Interfaces.ValidationResult
            {
                IsValid = false,
                Errors = errors
            };
        }
    }

    public async Task<ErrorOr<EmailTemplate[]>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = new List<EmailTemplate>();
            
            if (!Directory.Exists(_templatesPath))
            {
                return Array.Empty<EmailTemplate>();
            }

            var templateFiles = Directory.GetFiles(_templatesPath, "*.cshtml", SearchOption.AllDirectories);
            
            foreach (var file in templateFiles)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var relativePath = Path.GetRelativePath(_templatesPath, file);
                var category = Path.GetDirectoryName(relativePath) ?? "General";
                
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var requiredVars = await ExtractRequiredVariables(file);
                var templateVariables = requiredVars.Select(v => new TemplateVariable(v, "string", $"Variable {v}")).ToList();
                
                var template = new EmailTemplate(
                    name: name,
                    description: await ExtractDescriptionFromTemplate(file),
                    category: category,
                    content: content,
                    requiredVariables: templateVariables,
                    defaultSubject: await ExtractSubjectFromTemplate(file)
                );
                
                templates.Add(template);
            }

            return templates.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get templates");
            return Error.Failure($"Failed to get templates: {ex.Message}");
        }
    }

    public async Task<ErrorOr<EmailTemplate>> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            var templatePath = FindTemplatePath(templateName);
            if (templatePath == null)
            {
                return Error.NotFound($"Template '{templateName}' not found");
            }

            var category = Path.GetDirectoryName(Path.GetRelativePath(_templatesPath, templatePath)) ?? "General";
            
            var content = await File.ReadAllTextAsync(templatePath, cancellationToken);
            var requiredVars = await ExtractRequiredVariables(templatePath);
            var templateVariables = requiredVars.Select(v => new TemplateVariable(v, "string", $"Variable {v}")).ToList();
            
            return new EmailTemplate(
                name: templateName,
                description: await ExtractDescriptionFromTemplate(templatePath),
                category: category,
                content: content,
                requiredVariables: templateVariables,
                defaultSubject: await ExtractSubjectFromTemplate(templatePath)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateName}", templateName);
            return Error.Failure($"Failed to get template: {ex.Message}");
        }
    }

    private async Task<ErrorOr<RenderedTemplate>> RenderInternalAsync(
        EmailTemplate template,
        object model,
        string cacheKey,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
        // Convert dictionary to dynamic if needed
        object renderModel = model;
        if (model is Dictionary<string, object> dict)
        {
            var expando = new System.Dynamic.ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;
            foreach (var kvp in dict)
            {
                expandoDict[kvp.Key] = kvp.Value;
            }
            renderModel = expando;
        }

        // Render HTML from template content
        var templateKey = $"template_{template.Name}_{template.Content.GetHashCode()}";
        var htmlContent = await _razorEngine.CompileRenderStringAsync(templateKey, template.Content, renderModel);
        
        // Generate plain text version
        var textContent = GeneratePlainText(htmlContent);
        
        // Inline CSS for email clients
        var inlinedHtml = InlineCss(htmlContent);
        
        var rendered = new RenderedTemplate
        {
            HtmlContent = htmlContent,
            TextContent = textContent,
            InlinedHtmlContent = inlinedHtml,
            TemplateName = template.Name,
            RenderDuration = DateTime.UtcNow - startTime,
            FromCache = false
        };

        // Cache the result
        _cache.Set(cacheKey, rendered, TimeSpan.FromMinutes(_options.CacheDurationMinutes));
        
        return rendered;
    }

    private string InlineCss(string html)
    {
        try
        {
            var result = PreMailer.Net.PreMailer.MoveCssInline(html);
            return result.Html;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inline CSS, using original HTML");
            return html;
        }
    }

    private string GeneratePlainText(string html)
    {
        // Simple HTML to text conversion
        var text = html;
        
        // Remove style and script blocks
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Replace line breaks
        text = text.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
        text = text.Replace("</p>", "\n\n").Replace("</div>", "\n");
        
        // Remove all HTML tags
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");
        
        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);
        
        // Clean up whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        
        return text.Trim();
    }

    private void EnsureTemplatesDirectory()
    {
        if (!Directory.Exists(_templatesPath))
        {
            Directory.CreateDirectory(_templatesPath);
            _logger.LogInformation("Created templates directory at {Path}", _templatesPath);
            
            // Create a sample template
            CreateSampleTemplates();
        }
    }

    private void CreateSampleTemplates()
    {
        // Create welcome email template
        var welcomePath = Path.Combine(_templatesPath, "Account", "Welcome.cshtml");
        Directory.CreateDirectory(Path.GetDirectoryName(welcomePath)!);
        
        File.WriteAllText(welcomePath, @"@model dynamic
@{
    ViewBag.Subject = ""Welcome to DevOps MCP!"";
}
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #0066cc; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; }
        .button { display: inline-block; padding: 10px 20px; background-color: #0066cc; color: white; text-decoration: none; border-radius: 5px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to DevOps MCP</h1>
        </div>
        <div class=""content"">
            <p>Hi @Model.Name,</p>
            <p>Welcome to DevOps MCP! We're excited to have you on board.</p>
            <p>Your account has been created successfully. You can now start using our Azure DevOps integration tools.</p>
            <p><a href=""@Model.ActivationUrl"" class=""button"">Get Started</a></p>
            <p>If you have any questions, feel free to reach out to our support team.</p>
            <p>Best regards,<br>The DevOps MCP Team</p>
        </div>
    </div>
</body>
</html>");

        _logger.LogInformation("Created sample email templates");
    }

    private string? FindTemplatePath(string templateName)
    {
        // Handle both "Welcome" and "Account/Welcome" formats
        var possiblePaths = new List<string>
        {
            Path.Combine(_templatesPath, $"{templateName}.cshtml"),
            Path.Combine(_templatesPath, $"{templateName.Replace('/', Path.DirectorySeparatorChar)}.cshtml")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Search for just the filename in subdirectories
        var fileName = Path.GetFileName(templateName);
        var files = Directory.GetFiles(_templatesPath, $"{fileName}.cshtml", SearchOption.AllDirectories);
        
        // If multiple files found, prefer the one that matches the full path
        if (files.Length > 1 && templateName.Contains('/'))
        {
            var expectedPath = templateName.Replace('/', Path.DirectorySeparatorChar) + ".cshtml";
            return files.FirstOrDefault(f => f.EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase)) ?? files.FirstOrDefault();
        }
        
        return files.FirstOrDefault();
    }

    private async Task<string> ExtractSubjectFromTemplate(string templatePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(templatePath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"ViewBag\.Subject\s*=\s*""([^""]+)""");
            return match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(templatePath);
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(templatePath);
        }
    }

    private async Task<string> ExtractDescriptionFromTemplate(string templatePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(templatePath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"@\*\s*Description:\s*([^\*]+)\*@");
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }
        catch
        {
            return "";
        }
    }

    private async Task<List<string>> ExtractRequiredVariables(string templatePath)
    {
        var variables = new HashSet<string>();
        
        try
        {
            var content = await File.ReadAllTextAsync(templatePath);
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"@Model\.(\w+)");
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                variables.Add(match.Groups[1].Value);
            }
        }
        catch
        {
            // Ignore errors
        }

        return variables.ToList();
    }
}