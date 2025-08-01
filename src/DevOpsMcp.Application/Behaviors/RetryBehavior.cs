using Polly.Retry;

namespace DevOpsMcp.Application.Behaviors;

public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        
        _retryPolicy = Policy
            .Handle<Exception>(ex => IsTransientException(ex))
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var requestName = typeof(TRequest).Name;
                    _logger.LogWarning(
                        "Retrying {RequestName} after {TimeSpan}s (attempt {RetryCount})",
                        requestName,
                        timespan.TotalSeconds,
                        retryCount);
                });
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteAsync(async () => await next());
    }

    private static bool IsTransientException(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            _ => false
        };
    }
}