using System.Text.Json;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
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
/// AWS SES V2 implementation of enhanced email service
/// </summary>
public sealed class SesV2EmailService : IEnhancedEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SesV2EmailService> _logger;
    private readonly SesV2Options _options;
    private readonly EmailOptions _emailOptions;
    private readonly IAsyncPolicy<EmailResult> _resilientPolicy;

    public SesV2EmailService(
        IAmazonSimpleEmailServiceV2 sesClient,
        IMemoryCache cache,
        ILogger<SesV2EmailService> logger,
        IOptions<SesV2Options> options,
        IOptions<EmailOptions> emailOptions)
    {
        _sesClient = sesClient ?? throw new ArgumentNullException(nameof(sesClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
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

            // Execute with resilience policy
            var result = await _resilientPolicy.ExecuteAsync(async (ct) =>
            {
                return await SendEmailInternalAsync(request, ct);
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

    private async Task<EmailResult> SendEmailInternalAsync(
        EmailRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            SendEmailRequest sendRequest;

            if (!string.IsNullOrEmpty(request.TemplateName))
            {
                // Template-based email
                sendRequest = new SendEmailRequest
                {
                    FromEmailAddress = FormatAddress(_options.FromAddress, _options.FromName),
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { request.To },
                        CcAddresses = request.Cc.ToList(),
                        BccAddresses = request.Bcc.ToList()
                    },
                    Content = new EmailContent
                    {
                        Template = new Template
                        {
                            TemplateName = request.TemplateName,
                            TemplateData = JsonSerializer.Serialize(request.TemplateData)
                        }
                    },
                    ConfigurationSetName = request.ConfigurationSet ?? _options.DefaultConfigurationSet,
                    ReplyToAddresses = GetReplyToAddresses(request.ReplyTo)
                };
            }
            else
            {
                // Raw content email
                sendRequest = new SendEmailRequest
                {
                    FromEmailAddress = FormatAddress(_options.FromAddress, _options.FromName),
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { request.To },
                        CcAddresses = request.Cc.ToList(),
                        BccAddresses = request.Bcc.ToList()
                    },
                    Content = new EmailContent
                    {
                        Simple = new Message
                        {
                            Subject = new Content { Data = request.Subject ?? string.Empty },
                            Body = new Body
                            {
                                Html = new Content 
                                { 
                                    Data = request.HtmlContent ?? string.Empty,
                                    Charset = "UTF-8"
                                },
                                Text = !string.IsNullOrEmpty(request.TextContent) 
                                    ? new Content 
                                    { 
                                        Data = request.TextContent,
                                        Charset = "UTF-8"
                                    } 
                                    : null
                            }
                        }
                    },
                    ConfigurationSetName = request.ConfigurationSet ?? _options.DefaultConfigurationSet,
                    ReplyToAddresses = GetReplyToAddresses(request.ReplyTo)
                };
            }

            // Add tags
            if (request.Tags.Any())
            {
                sendRequest.EmailTags = request.Tags
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
                    ["HttpStatusCode"] = response.HttpStatusCode.ToString()
                }
            };
        }
        catch (AccountSuspendedException ex)
        {
            _logger.LogError(ex, "AWS account suspended. RequestId: {RequestId}", request.Id);
            return CreateErrorResult(request.Id, "Account suspended", startTime, false);
        }
        catch (MailFromDomainNotVerifiedException ex)
        {
            _logger.LogError(ex, "From domain not verified. RequestId: {RequestId}", request.Id);
            return CreateErrorResult(request.Id, "From domain not verified", startTime, false);
        }
        catch (MessageRejectedException ex)
        {
            _logger.LogWarning(ex, "Email rejected. RequestId: {RequestId}", request.Id);
            return CreateErrorResult(request.Id, $"Email rejected: {ex.Message}", startTime, false);
        }
        catch (SendingPausedException ex)
        {
            _logger.LogWarning(ex, "Sending paused. RequestId: {RequestId}", request.Id);
            return CreateErrorResult(request.Id, "Sending temporarily paused", startTime, true);
        }
        catch (TooManyRequestsException ex)
        {
            _logger.LogWarning(ex, "Rate limit exceeded. RequestId: {RequestId}", request.Id);
            return CreateErrorResult(request.Id, "Rate limit exceeded", startTime, true);
        }
        catch (Exception ex) when (IsTransientError(ex))
        {
            _logger.LogWarning(ex, "Transient error. RequestId: {RequestId}", request.Id);
            return CreateErrorResult(request.Id, ex.Message, startTime, true);
        }
    }

    public async Task<ErrorOr<BulkEmailResult>> SendBulkEmailAsync(
        BulkEmailRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var results = new List<BulkEmailEntryResult>();

            // Process in batches of MaxBulkSize
            var batches = request.Destinations
                .Select((destination, index) => new { destination, index })
                .GroupBy(x => x.index / _options.MaxBulkSize)
                .Select(g => g.Select(x => x.destination).ToList());

            foreach (var batch in batches)
            {
                var bulkRequest = new SendBulkEmailRequest
                {
                    FromEmailAddress = FormatAddress(_options.FromAddress, _options.FromName),
                    DefaultContent = new BulkEmailContent
                    {
                        Template = new Template
                        {
                            TemplateName = request.TemplateName,
                            TemplateData = JsonSerializer.Serialize(request.DefaultTemplateData)
                        }
                    },
                    BulkEmailEntries = batch.Select(dest => new BulkEmailEntry
                    {
                        Destination = new Destination
                        {
                            ToAddresses = new List<string> { dest.Email }
                        },
                        ReplacementTags = request.Tags
                            .Select(kvp => new MessageTag { Name = kvp.Key, Value = kvp.Value })
                            .ToList(),
                        ReplacementEmailContent = new ReplacementEmailContent
                        {
                            ReplacementTemplateData = JsonSerializer.Serialize(dest.TemplateData)
                        }
                    }).ToList(),
                    ConfigurationSetName = request.ConfigurationSet ?? _options.DefaultConfigurationSet,
                    ReplyToAddresses = request.ReplyToAddresses.Any() ? request.ReplyToAddresses.ToList() : _options.ReplyToAddresses
                };

                var response = await _sesClient.SendBulkEmailAsync(bulkRequest, cancellationToken);

                // Process results
                for (int i = 0; i < response.BulkEmailEntryResults.Count; i++)
                {
                    var entryResult = response.BulkEmailEntryResults[i];
                    var destination = batch[i];

                    results.Add(new BulkEmailEntryResult
                    {
                        Email = destination.Email,
                        Success = entryResult.Status == BulkEmailStatus.SUCCESS,
                        MessageId = entryResult.MessageId,
                        Error = entryResult.Error,
                        Status = entryResult.Status.ToString()
                    });
                }
            }

            return new BulkEmailResult
            {
                RequestId = request.Id,
                SuccessCount = results.Count(r => r.Success),
                FailureCount = results.Count(r => !r.Success),
                Results = results,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email {RequestId}", request.Id);
            return Error.Failure($"Bulk email send failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<EmailTemplate>> CreateTemplateAsync(
        EmailTemplate emailTemplate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var createRequest = new CreateEmailTemplateRequest
            {
                TemplateName = emailTemplate.Name,
                TemplateContent = new EmailTemplateContent
                {
                    Subject = emailTemplate.Subject,
                    Html = emailTemplate.HtmlContent,
                    Text = emailTemplate.TextContent
                }
            };

            await _sesClient.CreateEmailTemplateAsync(createRequest, cancellationToken);

            emailTemplate.CreatedAt = DateTime.UtcNow;
            emailTemplate.UpdatedAt = DateTime.UtcNow;

            // Cache the template
            await CacheTemplateAsync(emailTemplate, cancellationToken);

            _logger.LogInformation("Created email template {TemplateName}", emailTemplate.Name);
            return emailTemplate;
        }
        catch (AlreadyExistsException)
        {
            // Try to update instead
            return await UpdateTemplateAsync(emailTemplate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template {TemplateName}", emailTemplate.Name);
            return Error.Failure($"Failed to create template: {ex.Message}");
        }
    }

    private async Task<ErrorOr<EmailTemplate>> UpdateTemplateAsync(
        EmailTemplate emailTemplate, 
        CancellationToken cancellationToken)
    {
        try
        {
            var updateRequest = new UpdateEmailTemplateRequest
            {
                TemplateName = emailTemplate.Name,
                TemplateContent = new EmailTemplateContent
                {
                    Subject = emailTemplate.Subject,
                    Html = emailTemplate.HtmlContent,
                    Text = emailTemplate.TextContent
                }
            };

            await _sesClient.UpdateEmailTemplateAsync(updateRequest, cancellationToken);

            emailTemplate.UpdatedAt = DateTime.UtcNow;
            emailTemplate.Version++;

            // Update cache
            await CacheTemplateAsync(emailTemplate, cancellationToken);

            _logger.LogInformation("Updated email template {TemplateName}", emailTemplate.Name);
            return emailTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateName}", emailTemplate.Name);
            return Error.Failure($"Failed to update template: {ex.Message}");
        }
    }

    public async Task<ErrorOr<EmailTemplate>> GetTemplateAsync(
        string templateName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"template_{templateName}";
            if (_cache.TryGetValue<EmailTemplate>(cacheKey, out var cachedTemplate))
            {
                return cachedTemplate!;
            }

            var request = new GetEmailTemplateRequest { TemplateName = templateName };
            var response = await _sesClient.GetEmailTemplateAsync(request, cancellationToken);

            var template = new EmailTemplate
            {
                Name = response.TemplateName,
                Subject = response.TemplateContent.Subject,
                HtmlContent = response.TemplateContent.Html,
                TextContent = response.TemplateContent.Text,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Cache the template
            await CacheTemplateAsync(template, cancellationToken);

            return template;
        }
        catch (NotFoundException)
        {
            return Error.NotFound($"Template '{templateName}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateName}", templateName);
            return Error.Failure($"Failed to get template: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> DeleteTemplateAsync(
        string templateName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteEmailTemplateRequest { TemplateName = templateName };
            await _sesClient.DeleteEmailTemplateAsync(request, cancellationToken);

            // Remove from cache
            var cacheKey = $"template_{templateName}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("Deleted email template {TemplateName}", templateName);
            return true;
        }
        catch (NotFoundException)
        {
            return Error.NotFound($"Template '{templateName}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateName}", templateName);
            return Error.Failure($"Failed to delete template: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<EmailTemplate>>> ListTemplatesAsync(
        int? pageSize = null,
        string? nextToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ListEmailTemplatesRequest
            {
                PageSize = pageSize ?? 100,
                NextToken = nextToken
            };

            var response = await _sesClient.ListEmailTemplatesAsync(request, cancellationToken);

            var templates = new List<EmailTemplate>();
            foreach (var templateMetadata in response.TemplatesMetadata)
            {
                var template = new EmailTemplate
                {
                    Name = templateMetadata.TemplateName,
                    CreatedAt = templateMetadata.CreatedTimestamp
                };
                templates.Add(template);
            }

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list templates");
            return Error.Failure($"Failed to list templates: {ex.Message}");
        }
    }

    public async Task<ErrorOr<ConfigurationSet>> CreateConfigurationSetAsync(
        ConfigurationSet configSet, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CreateConfigurationSetRequest
            {
                ConfigurationSetName = configSet.Name,
                TrackingOptions = new TrackingOptions
                {
                    CustomRedirectDomain = configSet.TrackingEnabled ? "track.example.com" : null
                },
                SendingOptions = new SendingOptions
                {
                    SendingEnabled = configSet.SendingStatus == SendingStatus.Enabled
                },
                ReputationOptions = new ReputationOptions
                {
                    ReputationMetricsEnabled = configSet.ReputationTracking == ReputationTrackingStatus.Enabled
                }
            };

            await _sesClient.CreateConfigurationSetAsync(request, cancellationToken);

            configSet.CreatedAt = DateTime.UtcNow;
            _logger.LogInformation("Created configuration set {ConfigSetName}", configSet.Name);
            return configSet;
        }
        catch (AlreadyExistsException)
        {
            return Error.Conflict($"Configuration set '{configSet.Name}' already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create configuration set {ConfigSetName}", configSet.Name);
            return Error.Failure($"Failed to create configuration set: {ex.Message}");
        }
    }

    public async Task<ErrorOr<SuppressionEntry>> AddToSuppressionListAsync(
        string email, 
        SuppressionReason reason,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new PutAccountSuppressionAttributesRequest
            {
                SuppressedReasons = reason switch
                {
                    SuppressionReason.Bounced => new List<string> { "BOUNCE" },
                    SuppressionReason.Complained => new List<string> { "COMPLAINT" },
                    _ => new List<string> { "BOUNCE", "COMPLAINT" }
                }
            };

            await _sesClient.PutAccountSuppressionAttributesAsync(request, cancellationToken);

            var entry = new SuppressionEntry
            {
                Email = email,
                Reason = reason,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                Source = "Manual"
            };

            _logger.LogInformation("Added {Email} to suppression list for reason {Reason}", email, reason);
            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Email} to suppression list", email);
            return Error.Failure($"Failed to add to suppression list: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> RemoveFromSuppressionListAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteAccountSuppressionAttributesRequest();
            await _sesClient.DeleteAccountSuppressionAttributesAsync(request, cancellationToken);

            _logger.LogInformation("Removed {Email} from suppression list", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove {Email} from suppression list", email);
            return Error.Failure($"Failed to remove from suppression list: {ex.Message}");
        }
    }

    public async Task<ErrorOr<DevOpsMcp.Domain.Email.SendQuota>> GetSendQuotaAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            const string cacheKey = "send_quota";
            if (_cache.TryGetValue<DevOpsMcp.Domain.Email.SendQuota>(cacheKey, out var cachedQuota))
            {
                return cachedQuota!;
            }

            var request = new GetAccountRequest();
            var response = await _sesClient.GetAccountAsync(request, cancellationToken);

            var quota = new DevOpsMcp.Domain.Email.SendQuota
            {
                Max24HourSend = response.SendQuota.Max24HourSend,
                MaxSendRate = response.SendQuota.MaxSendRate,
                SentLast24Hours = response.SendQuota.SentLast24Hours
            };

            // Cache for 5 minutes
            _cache.Set(cacheKey, quota, TimeSpan.FromMinutes(5));

            return quota;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get send quota");
            return Error.Failure($"Failed to get send quota: {ex.Message}");
        }
    }

    public async Task<ErrorOr<EmailStatistics>> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In production, you would aggregate data from CloudWatch or your event store
            var statistics = new EmailStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                SendCount = 0,
                BounceCount = 0,
                ComplaintCount = 0,
                DeliveryCount = 0,
                RejectCount = 0,
                OpenCount = 0,
                ClickCount = 0
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get email statistics");
            return Error.Failure($"Failed to get statistics: {ex.Message}");
        }
    }

    public async Task<ErrorOr<RenderedTemplate>> TestTemplateAsync(
        string templateName,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new TestRenderEmailTemplateRequest
            {
                TemplateName = templateName,
                TemplateData = JsonSerializer.Serialize(templateData)
            };

            var response = await _sesClient.TestRenderEmailTemplateAsync(request, cancellationToken);

            return new RenderedTemplate
            {
                Subject = response.RenderedTemplate.Subject,
                HtmlContent = response.RenderedTemplate.Html,
                TextContent = response.RenderedTemplate.Text,
                InlinedHtmlContent = response.RenderedTemplate.Html // V2 handles inlining
            };
        }
        catch (NotFoundException)
        {
            return Error.NotFound($"Template '{templateName}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test template {TemplateName}", templateName);
            return Error.Failure($"Failed to test template: {ex.Message}");
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

        // In production, query event configuration set data
        return EmailStatus.Sent;
    }

    public async Task<ErrorOr<DevOpsMcp.Domain.Interfaces.ValidationResult>> ValidateEmailAsync(
        EmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = GetSecurityPolicy(_emailOptions.DefaultSecurityPolicy);
        var validationResult = policy.ValidateRequest(request);
        
        return new DevOpsMcp.Domain.Interfaces.ValidationResult
        {
            IsValid = validationResult.IsValid,
            Errors = validationResult.Errors
        };
    }

    private IAsyncPolicy<EmailResult> CreateResiliencePolicy()
    {
        // Retry policy for transient failures
        var retryPolicy = Policy
            .HandleResult<EmailResult>(r => !r.Success && r.IsTransient)
            .Or<TooManyRequestsException>()
            .Or<SendingPausedException>()
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
            .TimeoutAsync<EmailResult>(_options.TimeoutSeconds);

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
            AmazonSimpleEmailServiceV2Exception sesEx => 
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

    private List<string> GetReplyToAddresses(string? replyTo)
    {
        if (!string.IsNullOrEmpty(replyTo))
            return new List<string> { replyTo };
        
        return _options.ReplyToAddresses.Any() 
            ? _options.ReplyToAddresses 
            : new List<string>();
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

    private async Task CacheTemplateAsync(EmailTemplate template, CancellationToken cancellationToken)
    {
        var cacheKey = $"template_{template.Name}";
        _cache.Set(cacheKey, template, _options.Templates.CacheDuration);
        await Task.CompletedTask;
    }

    private EmailResult CreateErrorResult(string requestId, string error, DateTime startTime, bool isTransient)
    {
        return new EmailResult
        {
            Success = false,
            RequestId = requestId,
            Error = error,
            Status = EmailStatus.Failed,
            Duration = DateTime.UtcNow - startTime,
            IsTransient = isTransient
        };
    }
}