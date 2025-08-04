using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Application.Email.Commands;

/// <summary>
/// Handler for SendEmailCommand
/// </summary>
public sealed class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, ErrorOr<EmailResult>>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(
        IEmailService emailService,
        ILogger<SendEmailCommandHandler> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<EmailResult>> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending email to {To} with template {Template}", 
                request.To, request.TemplateName);

            var emailRequest = EmailRequest.FromTemplate(
                to: request.To,
                templateName: request.TemplateName,
                templateData: request.TemplateData,
                cc: request.Cc.ToList(),
                bcc: request.Bcc.ToList(),
                replyTo: request.ReplyTo,
                priority: request.Priority,
                tags: request.Tags);

            // Send with optional security policy override
            ErrorOr<EmailResult> result;
            if (!string.IsNullOrEmpty(request.SecurityPolicy))
            {
                var policy = ParseSecurityPolicy(request.SecurityPolicy);
                result = await _emailService.SendEmailAsync(emailRequest, policy, cancellationToken);
            }
            else
            {
                result = await _emailService.SendEmailAsync(emailRequest, cancellationToken);
            }

            if (result.IsError)
            {
                _logger.LogWarning("Failed to send email: {Errors}", 
                    string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", 
                    result.Value.MessageId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email");
            return Error.Failure($"Failed to send email: {ex.Message}");
        }
    }

    private EmailSecurityPolicy ParseSecurityPolicy(string policyName)
    {
        return policyName?.ToLowerInvariant() switch
        {
            "development" => EmailSecurityPolicy.Development,
            "standard" => EmailSecurityPolicy.Standard,
            "restricted" => EmailSecurityPolicy.Restricted,
            _ => EmailSecurityPolicy.Standard
        };
    }
}