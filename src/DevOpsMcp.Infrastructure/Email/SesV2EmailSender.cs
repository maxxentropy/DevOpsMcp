using System.Text.Json;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Infrastructure.Email.Builders;
using ErrorOr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace DevOpsMcp.Infrastructure.Email;

/// <summary>
/// Clean implementation of AWS SES V2 email sending service
/// </summary>
public sealed class SesV2EmailSender : IEnhancedEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SesV2EmailSender> _logger;
    private readonly SesV2Options _options;
    private readonly EmailOptions _emailOptions;
    private readonly SendEmailRequestBuilder _requestBuilder;
    private readonly IAsyncPolicy<EmailResult> _resilientPolicy;

    public SesV2EmailSender(
        IAmazonSimpleEmailServiceV2 sesClient,
        IMemoryCache cache,
        ILogger<SesV2EmailSender> logger,
        IOptions<SesV2Options> options,
        IOptions<EmailOptions> emailOptions)
    {
        _sesClient = sesClient ?? throw new ArgumentNullException(nameof(sesClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _emailOptions = emailOptions.Value ?? throw new ArgumentNullException(nameof(emailOptions));
        
        _requestBuilder = new SendEmailRequestBuilder(
            _options.FromAddress,
            _options.FromName,
            _options.DefaultConfigurationSet,
            _options.ReplyToAddresses);
            
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
            // Validate request
            var validationResult = await ValidateRequestAsync(request, policy, cancellationToken);
            if (validationResult.IsError)
            {
                return validationResult.Errors;
            }

            // Send with resilience policy
            var result = await _resilientPolicy.ExecuteAsync(
                async (ct) => await SendEmailInternalAsync(request, ct),
                cancellationToken);

            // Cache result if successful
            if (result.Success && !string.IsNullOrEmpty(result.MessageId))
            {
                await CacheEmailStatusAsync(result.MessageId, EmailStatus.Sent);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email {RequestId}", request.Id);
            return Error.Failure($"Email send failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<EmailResult>> SendTemplatedEmailAsync(
        string toAddress,
        string templateName,
        Dictionary<string, object> templateData,
        string? configurationSet = null,
        List<string>? cc = null,
        List<string>? bcc = null,
        string? replyTo = null,
        Dictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var request = EmailRequest.FromTemplate(
            to: toAddress,
            templateName: templateName,
            templateData: templateData,
            cc: cc,
            bcc: bcc,
            replyTo: replyTo,
            tags: tags,
            configurationSet: configurationSet);

        return await SendEmailAsync(request, cancellationToken);
    }

    public async Task<ErrorOr<BulkEmailResult>> SendBulkEmailAsync(
        BulkEmailRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var allResults = new List<DevOpsMcp.Domain.Email.BulkEmailEntryResult>();

            // Process in batches
            var batches = CreateBatches(request.Destinations, _options.MaxBulkSize);
            
            foreach (var batch in batches)
            {
                var batchResults = await SendBulkBatchAsync(request, batch, cancellationToken);
                allResults.AddRange(batchResults);
            }

            return new BulkEmailResult
            {
                RequestId = request.Id,
                SuccessCount = allResults.Count(r => r.Success),
                FailureCount = allResults.Count(r => !r.Success),
                Results = allResults,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email {RequestId}", request.Id);
            return Error.Failure($"Bulk email send failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<EmailStatus>> GetEmailStatusAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"email_status_{messageId}";
        if (_cache.TryGetValue<EmailStatus>(cacheKey, out var cachedStatus))
        {
            return cachedStatus;
        }

        // In a full implementation, this would query event data
        return EmailStatus.Sent;
    }

    public async Task<ErrorOr<DevOpsMcp.Domain.Interfaces.ValidationResult>> ValidateEmailAsync(
        EmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = GetSecurityPolicy(_emailOptions.DefaultSecurityPolicy);
        var result = await ValidateRequestAsync(request, policy, cancellationToken);
        
        if (result.IsError)
        {
            return new DevOpsMcp.Domain.Interfaces.ValidationResult
            {
                IsValid = false,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        return new DevOpsMcp.Domain.Interfaces.ValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };
    }

    private async Task<EmailResult> SendEmailInternalAsync(
        EmailRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var sendRequest = _requestBuilder.Build(request);
            var response = await _sesClient.SendEmailAsync(sendRequest, cancellationToken);
            
            _logger.LogInformation(
                "Email sent successfully. RequestId: {RequestId}, MessageId: {MessageId}", 
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
        catch (Exception ex)
        {
            var error = MapExceptionToError(ex);
            
            return new EmailResult
            {
                Success = false,
                RequestId = request.Id,
                Error = error.Message,
                Status = EmailStatus.Failed,
                Duration = DateTime.UtcNow - startTime,
                IsTransient = error.IsTransient
            };
        }
    }

    private async Task<List<DevOpsMcp.Domain.Email.BulkEmailEntryResult>> SendBulkBatchAsync(
        BulkEmailRequest request,
        List<BulkEmailDestination> batch,
        CancellationToken cancellationToken)
    {
        var bulkRequest = BuildBulkEmailRequest(request, batch);
        var response = await _sesClient.SendBulkEmailAsync(bulkRequest, cancellationToken);

        var results = new List<DevOpsMcp.Domain.Email.BulkEmailEntryResult>();
        
        for (int i = 0; i < response.BulkEmailEntryResults.Count; i++)
        {
            var entryResult = response.BulkEmailEntryResults[i];
            var destination = batch[i];

            results.Add(new DevOpsMcp.Domain.Email.BulkEmailEntryResult
            {
                Email = destination.Email,
                Success = entryResult.Status == BulkEmailStatus.SUCCESS,
                MessageId = entryResult.MessageId,
                Error = entryResult.Error,
                Status = entryResult.Status.ToString()
            });
        }

        return results;
    }

    private SendBulkEmailRequest BuildBulkEmailRequest(
        BulkEmailRequest request, 
        List<BulkEmailDestination> batch)
    {
        return new SendBulkEmailRequest
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
            BulkEmailEntries = batch.Select(dest => 
            {
                var entry = new BulkEmailEntry
                {
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { dest.Email }
                    },
                    ReplacementTags = request.Tags
                        .Select(kvp => new MessageTag { Name = kvp.Key, Value = kvp.Value })
                        .ToList()
                };
                
                // Note: AWS SES V2 doesn't support per-recipient template data in bulk sends
                // All recipients in a bulk send use the same template data
                // For per-recipient customization, use multiple SendEmailAsync calls instead
                
                return entry;
            }).ToList(),
            ConfigurationSetName = request.ConfigurationSet ?? _options.DefaultConfigurationSet,
            ReplyToAddresses = request.ReplyToAddresses.Any() 
                ? request.ReplyToAddresses.ToList() 
                : _options.ReplyToAddresses
        };
    }

    private async Task<ErrorOr<bool>> ValidateRequestAsync(
        EmailRequest request,
        EmailSecurityPolicy policy,
        CancellationToken cancellationToken)
    {
        var validationResult = policy.ValidateRequest(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Email request {RequestId} failed validation: {Errors}", 
                request.Id, 
                string.Join(", ", validationResult.Errors));
                
            return Error.Validation($"Email validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        // Additional async validations could go here (e.g., checking suppression list)
        await Task.CompletedTask;
        
        return true;
    }

    private IAsyncPolicy<EmailResult> CreateResiliencePolicy()
    {
        var retryPolicy = Policy
            .HandleResult<EmailResult>(r => !r.Success && r.IsTransient)
            .Or<TooManyRequestsException>()
            .Or<SendingPausedException>()
            .WaitAndRetryAsync(
                _emailOptions.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s for email request", 
                        retryCount, 
                        timespan.TotalSeconds);
                });

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

        var timeoutPolicy = Policy
            .TimeoutAsync<EmailResult>(_options.TimeoutSeconds);

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
                });

        return Policy.WrapAsync(fallbackPolicy, circuitBreakerPolicy, retryPolicy, timeoutPolicy);
    }

    private static List<List<T>> CreateBatches<T>(IReadOnlyList<T> items, int batchSize)
    {
        var batches = new List<List<T>>();
        
        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            batches.Add(batch);
        }
        
        return batches;
    }

    private (string Message, bool IsTransient) MapExceptionToError(Exception ex)
    {
        return ex switch
        {
            AccountSuspendedException => ("AWS account suspended", false),
            MailFromDomainNotVerifiedException => ("From domain not verified", false),
            MessageRejectedException msgEx => ($"Email rejected: {msgEx.Message}", false),
            SendingPausedException => ("Sending temporarily paused", true),
            TooManyRequestsException => ("Rate limit exceeded", true),
            AmazonSimpleEmailServiceV2Exception sesEx when IsTransientError(sesEx) => (sesEx.Message, true),
            _ => (ex.Message, false)
        };
    }

    private static bool IsTransientError(AmazonSimpleEmailServiceV2Exception ex)
    {
        return ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
               ex.StatusCode == System.Net.HttpStatusCode.RequestTimeout;
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

    private async Task CacheEmailStatusAsync(string messageId, EmailStatus status)
    {
        var cacheKey = $"email_status_{messageId}";
        _cache.Set(cacheKey, status, TimeSpan.FromMinutes(60));
        await Task.CompletedTask;
    }

    private static string FormatAddress(string email, string? name)
    {
        return string.IsNullOrEmpty(name) 
            ? email 
            : $"\"{name}\" <{email}>";
    }
}