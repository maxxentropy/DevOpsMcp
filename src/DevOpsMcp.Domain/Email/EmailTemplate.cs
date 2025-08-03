using System;
using System.Collections.Generic;

namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Represents an email template
/// </summary>
public sealed class EmailTemplate
{
    /// <summary>
    /// Unique template identifier
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Template name (used in email requests)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Template category (e.g., "notifications", "reports", "alerts")
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Razor template content
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Layout template to use (optional)
    /// </summary>
    public string? LayoutName { get; }

    /// <summary>
    /// Default subject line (can be overridden)
    /// </summary>
    public string? DefaultSubject { get; }

    /// <summary>
    /// Required template variables
    /// </summary>
    public IReadOnlyList<TemplateVariable> RequiredVariables { get; }

    /// <summary>
    /// Optional template variables with defaults
    /// </summary>
    public IReadOnlyDictionary<string, object> DefaultVariables { get; }

    /// <summary>
    /// Template version
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// When this template was created
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// When this template was last modified
    /// </summary>
    public DateTime? ModifiedAt { get; }

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool IsActive { get; }

    public EmailTemplate(
        string name,
        string description,
        string category,
        string content,
        List<TemplateVariable>? requiredVariables = null,
        Dictionary<string, object>? defaultVariables = null,
        string? layoutName = null,
        string? defaultSubject = null,
        string? version = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Template content is required", nameof(content));

        Id = Guid.NewGuid().ToString();
        Name = name;
        Description = description;
        Category = category;
        Content = content;
        RequiredVariables = requiredVariables ?? new List<TemplateVariable>();
        DefaultVariables = defaultVariables ?? new Dictionary<string, object>();
        LayoutName = layoutName;
        DefaultSubject = defaultSubject;
        Version = version ?? "1.0.0";
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
}

/// <summary>
/// Represents a template variable
/// </summary>
public sealed class TemplateVariable
{
    /// <summary>
    /// Variable name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Variable type (string, number, boolean, object)
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Variable description
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Whether this variable is required
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Default value if not provided
    /// </summary>
    public object? DefaultValue { get; }

    public TemplateVariable(
        string name,
        string type,
        string description,
        bool isRequired = true,
        object? defaultValue = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }
}