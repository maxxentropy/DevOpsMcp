using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DevOpsMcp.Infrastructure.Email;

/// <summary>
/// Minimal AWS SES V2 email service implementation
/// Leverages AWS's built-in infrastructure for reliability, scaling, and monitoring
/// </summary>
public sealed class SesV2EmailSender : IEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;
    private readonly ILogger<SesV2EmailSender> _logger;
    private readonly SesV2Options _options;

    public SesV2EmailSender(
        IAmazonSimpleEmailServiceV2 sesClient,
        ILogger<SesV2EmailSender> logger,
        IOptions<SesV2Options> options)
    {
        _sesClient = sesClient ?? throw new ArgumentNullException(nameof(sesClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<EmailResult>> SendEmailAsync(
        string toAddress, 
        string subject, 
        string body, 
        bool isHtml = true,
        List<string>? cc = null,
        List<string>? bcc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SendEmailRequest
            {
                FromEmailAddress = FormatFromAddress(),
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toAddress },
                    CcAddresses = cc ?? new List<string>(),
                    BccAddresses = bcc ?? new List<string>()
                },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content { Data = subject, Charset = "UTF-8" },
                        Body = new Body
                        {
                            Html = isHtml ? new Content { Data = body, Charset = "UTF-8" } : null,
                            Text = !isHtml ? new Content { Data = body, Charset = "UTF-8" } : null
                        }
                    }
                }
            };

            // Add configuration set if configured
            if (!string.IsNullOrEmpty(_options.DefaultConfigurationSet))
            {
                request.ConfigurationSetName = _options.DefaultConfigurationSet;
            }

            // Add reply-to if configured
            if (_options.ReplyToAddresses?.Any() == true)
            {
                request.ReplyToAddresses = _options.ReplyToAddresses.ToList();
            }

            var response = await _sesClient.SendEmailAsync(request, cancellationToken);

            _logger.LogInformation("Email sent successfully to {ToAddress}. MessageId: {MessageId}", 
                toAddress, response.MessageId);

            return new EmailResult
            {
                Success = true,
                RequestId = Guid.NewGuid().ToString(),
                MessageId = response.MessageId,
                Status = EmailStatus.Sent,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress}", toAddress);
            return Error.Failure(ex.Message);
        }
    }

    public async Task<ErrorOr<EmailResult>> SendTemplatedEmailAsync(
        string toAddress,
        string templateName,
        Dictionary<string, object> templateData,
        List<string>? cc = null,
        List<string>? bcc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SendEmailRequest
            {
                FromEmailAddress = FormatFromAddress(),
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toAddress },
                    CcAddresses = cc ?? new List<string>(),
                    BccAddresses = bcc ?? new List<string>()
                },
                Content = new EmailContent
                {
                    Template = new Template
                    {
                        TemplateName = templateName,
                        TemplateData = JsonSerializer.Serialize(templateData)
                    }
                }
            };

            // Add configuration set if configured
            if (!string.IsNullOrEmpty(_options.DefaultConfigurationSet))
            {
                request.ConfigurationSetName = _options.DefaultConfigurationSet;
            }

            var response = await _sesClient.SendEmailAsync(request, cancellationToken);

            _logger.LogInformation("Templated email sent successfully to {ToAddress} using template {TemplateName}. MessageId: {MessageId}", 
                toAddress, templateName, response.MessageId);

            return new EmailResult
            {
                Success = true,
                RequestId = Guid.NewGuid().ToString(),
                MessageId = response.MessageId,
                Status = EmailStatus.Sent,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email to {ToAddress} using template {TemplateName}", 
                toAddress, templateName);
            return Error.Failure($"Failed to send templated email: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<EmailResult>>> SendTeamEmailAsync(
        List<string> teamEmails,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EmailResult>();
        var errors = new List<Error>();

        // Send individual emails to each team member
        // AWS SES handles rate limiting automatically
        foreach (var email in teamEmails)
        {
            var result = await SendEmailAsync(email, subject, body, isHtml, 
                cancellationToken: cancellationToken);
            
            if (result.IsError)
            {
                errors.AddRange(result.Errors);
                _logger.LogWarning("Failed to send team email to {Email}", email);
            }
            else
            {
                results.Add(result.Value);
            }
        }

        if (results.Count == 0 && errors.Count > 0)
        {
            return errors;
        }

        _logger.LogInformation("Team email sent to {SuccessCount}/{TotalCount} recipients", 
            results.Count, teamEmails.Count);

        return results;
    }

    private string FormatFromAddress()
    {
        return string.IsNullOrEmpty(_options.FromName) 
            ? _options.FromAddress 
            : $"\"{_options.FromName}\" <{_options.FromAddress}>";
    }
}