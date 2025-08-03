using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using ErrorOr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace DevOpsMcp.Infrastructure.Email;

/// <summary>
/// AWS SES implementation of email service
/// </summary>
public sealed class SesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SesEmailService> _logger;
    private readonly AwsSesOptions _sesOptions;
    private readonly EmailOptions _emailOptions;
    private readonly IAsyncPolicy<EmailResult> _resilientPolicy;

    public SesEmailService(
        IAmazonSimpleEmailService sesClient,
        IEmailTemplateRenderer templateRenderer,
        IMemoryCache cache,
        ILogger<SesEmailService> logger,
        IOptions<AwsSesOptions> sesOptions,
        IOptions<EmailOptions> emailOptions)
    {
        _sesClient = sesClient ?? throw new ArgumentNullException(nameof(sesClient));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sesOptions = sesOptions.Value ?? throw new ArgumentNullException(nameof(sesOptions));
        _emailOptions = emailOptions.Value ?? throw new ArgumentNullException(nameof(emailOptions));
        
        _resilientPolicy = CreateResiliencePolicy();
    }

    public async Task<ErrorOr<EmailResult>> SendEmailAsync(
        EmailRequest request, 
        CancellationToken cancellationToken = default)
    {
        var policy = GetSecurityPolicy(_emailOptions.DefaultSecurityPolicy);
        return await SendEmailAsync(request, policy, cancellationToken);
    }

    public async Task<ErrorOr<EmailResult>> SendEmailAsync(
        EmailRequest request,
        EmailSecurityPolicy policy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request against security policy
            var validationResult = policy.ValidateRequest(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Email request {RequestId} failed validation: {Errors}", 
                    request.Id, string.Join(", ", validationResult.Errors));
                
                return Error.Validation($"Email validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // Render template
            var renderResult = await _templateRenderer.RenderAsync(request.TemplateName, request.TemplateData, cancellationToken);
            if (renderResult.IsError)
            {
                return renderResult.Errors;
            }

            var rendered = renderResult.Value;

            // Execute with resilience policy
            var result = await _resilientPolicy.ExecuteAsync(async (ct) =>
            {
                return await SendEmailInternalAsync(request, rendered, ct);
            }, cancellationToken);

            // Cache successful result
            if (result.Success && !string.IsNullOrEmpty(result.MessageId))
            {
                await CacheEmailStatusAsync(result.MessageId, EmailStatus.Sent, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email {RequestId}", request.Id);
            return Error.Failure($"Email send failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<EmailStatus>> GetEmailStatusAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"email_status_{messageId}";
        if (_cache.TryGetValue<EmailStatus>(cacheKey, out var cachedStatus))
        {
            return cachedStatus;
        }

        // In production, you would query SES event configuration set data
        // For now, return a default status
        return EmailStatus.Sent;
    }

    public async Task<ErrorOr<DevOpsMcp.Domain.Interfaces.ValidationResult>> ValidateEmailAsync(
        EmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = GetSecurityPolicy(_emailOptions.DefaultSecurityPolicy);
        var validationResult = policy.ValidateRequest(request);
        
        // Additional async validations could go here (e.g., checking suppression list)
        await Task.CompletedTask;
        
        // Convert to the interface's ValidationResult type
        return new DevOpsMcp.Domain.Interfaces.ValidationResult
        {
            IsValid = validationResult.IsValid,
            Errors = validationResult.Errors
        };
    }

    private async Task<EmailResult> SendEmailInternalAsync(
        EmailRequest request,
        RenderedTemplate rendered,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var sendRequest = new SendEmailRequest
            {
                Source = FormatAddress(_sesOptions.FromAddress, _sesOptions.FromName),
                Destination = new Destination
                {
                    ToAddresses = new List<string> { request.To },
                    CcAddresses = request.Cc.ToList(),
                    BccAddresses = request.Bcc.ToList()
                },
                Message = new Message
                {
                    Subject = new Content(request.Subject),
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = rendered.InlinedHtmlContent
                        },
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            Data = rendered.TextContent
                        }
                    }
                },
                ConfigurationSetName = !string.IsNullOrEmpty(_sesOptions.ConfigurationSet) ? _sesOptions.ConfigurationSet : null,
                ReplyToAddresses = !string.IsNullOrEmpty(request.ReplyTo) 
                    ? new List<string> { request.ReplyTo }
                    : !string.IsNullOrEmpty(_sesOptions.ReplyToAddress)
                        ? new List<string> { _sesOptions.ReplyToAddress }
                        : null
            };

            // Add tags for tracking
            if (request.Tags.Any())
            {
                sendRequest.Tags = request.Tags
                    .Select(kvp => new MessageTag { Name = kvp.Key, Value = kvp.Value })
                    .ToList();
            }

            var response = await _sesClient.SendEmailAsync(sendRequest, cancellationToken);
            
            _logger.LogInformation("Email sent successfully. RequestId: {RequestId}, MessageId: {MessageId}", 
                request.Id, response.MessageId);

            return new EmailResult
            {
                Success = true,
                RequestId = request.Id,
                MessageId = response.MessageId,
                Status = EmailStatus.Sent,
                Duration = DateTime.UtcNow - startTime,
                Metadata = new Dictionary<string, string>
                {
                    ["SesRequestId"] = response.ResponseMetadata.RequestId,
                    ["HttpStatusCode"] = response.HttpStatusCode.ToString()
                }
            };
        }
        catch (MessageRejectedException ex)
        {
            _logger.LogWarning(ex, "Email rejected by SES. RequestId: {RequestId}", request.Id);
            return new EmailResult
            {
                Success = false,
                RequestId = request.Id,
                Error = $"Email rejected: {ex.Message}",
                Status = EmailStatus.Failed,
                Duration = DateTime.UtcNow - startTime,
                IsTransient = false
            };
        }
        catch (MailFromDomainNotVerifiedException ex)
        {
            _logger.LogError(ex, "From domain not verified. RequestId: {RequestId}", request.Id);
            return new EmailResult
            {
                Success = false,
                RequestId = request.Id,
                Error = "From domain not verified in SES",
                Status = EmailStatus.Failed,
                Duration = DateTime.UtcNow - startTime,
                IsTransient = false
            };
        }
        catch (Exception ex) when (IsTransientError(ex))
        {
            _logger.LogWarning(ex, "Transient error sending email. RequestId: {RequestId}", request.Id);
            return new EmailResult
            {
                Success = false,
                RequestId = request.Id,
                Error = ex.Message,
                Status = EmailStatus.Failed,
                Duration = DateTime.UtcNow - startTime,
                IsTransient = true
            };
        }
    }

    private IAsyncPolicy<EmailResult> CreateResiliencePolicy()
    {
        // Retry policy for transient failures
        var retryPolicy = Policy
            .HandleResult<EmailResult>(r => !r.Success && r.IsTransient)
            .Or<AmazonSimpleEmailServiceException>(ex => IsTransientError(ex))
            .WaitAndRetryAsync(
                _emailOptions.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var requestId = context.TryGetValue("RequestId", out var requestIdValue) 
                        ? requestIdValue.ToString() 
                        : "Unknown";
                    
                    _logger.LogWarning("Retry {RetryCount} after {Delay}s for request {RequestId}", 
                        retryCount, timespan.TotalSeconds, requestId);
                });

        // Circuit breaker for persistent failures
        var circuitBreakerPolicy = Policy
            .HandleResult<EmailResult>(r => !r.Success)
            .CircuitBreakerAsync(
                _emailOptions.CircuitBreakerThreshold,
                TimeSpan.FromSeconds(_emailOptions.CircuitBreakerDurationSeconds),
                onBreak: (result, duration) => 
                {
                    _logger.LogError("Email service circuit breaker opened for {Duration}", duration);
                },
                onReset: () => 
                {
                    _logger.LogInformation("Email service circuit breaker reset");
                });

        // Timeout policy
        var timeoutPolicy = Policy
            .TimeoutAsync<EmailResult>(_sesOptions.TimeoutSeconds);

        // Fallback for circuit breaker
        var fallbackPolicy = Policy<EmailResult>
            .Handle<BrokenCircuitException>()
            .FallbackAsync(
                new EmailResult 
                { 
                    Success = false, 
                    RequestId = string.Empty,
                    Error = "Email service temporarily unavailable",
                    Status = EmailStatus.Failed,
                    IsTransient = true
                },
                onFallbackAsync: (result, context) =>
                {
                    _logger.LogWarning("Email service fallback triggered");
                    return Task.CompletedTask;
                });

        return Policy.WrapAsync(fallbackPolicy, circuitBreakerPolicy, retryPolicy, timeoutPolicy);
    }

    private bool IsTransientError(Exception ex)
    {
        return ex switch
        {
            AmazonSimpleEmailServiceException sesEx => 
                sesEx.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                sesEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                sesEx.StatusCode == System.Net.HttpStatusCode.RequestTimeout,
            TaskCanceledException => true,
            _ => false
        };
    }

    private string FormatAddress(string email, string? name)
    {
        return string.IsNullOrEmpty(name) 
            ? email 
            : $"\"{name}\" <{email}>";
    }

    private EmailSecurityPolicy GetSecurityPolicy(string policyName)
    {
        return policyName?.ToLowerInvariant() switch
        {
            "development" => EmailSecurityPolicy.Development,
            "standard" => EmailSecurityPolicy.Standard,
            "restricted" => EmailSecurityPolicy.Restricted,
            _ => EmailSecurityPolicy.Standard
        };
    }

    private async Task CacheEmailStatusAsync(string messageId, EmailStatus status, CancellationToken cancellationToken)
    {
        var cacheKey = $"email_status_{messageId}";
        _cache.Set(cacheKey, status, TimeSpan.FromMinutes(60));
        await Task.CompletedTask;
    }
}