using Microsoft.Extensions.Logging;
using RetireRight.Core.Configuration;

namespace RetireRight.Scrapers.AntiBot;

public class RetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(ILogger<RetryPolicy> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        RetryOptions options,
        string operationName,
        CancellationToken ct = default)
    {
        var delay = options.InitialDelayMs;

        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < options.MaxAttempts && IsRetryable(ex))
            {
                _logger.LogWarning(
                    "Attempt {Attempt}/{Max} failed for {Operation}: {Error}. Retrying in {Delay}ms",
                    attempt, options.MaxAttempts, operationName, ex.Message, delay);

                await Task.Delay(delay, ct);
                delay = (int)(delay * options.BackoffMultiplier);
            }
        }

        // Final attempt — let exception propagate
        return await action();
    }

    private static bool IsRetryable(Exception ex) => ex switch
    {
        HttpRequestException httpEx => httpEx.StatusCode switch
        {
            System.Net.HttpStatusCode.TooManyRequests => true,      // 429
            System.Net.HttpStatusCode.InternalServerError => true,  // 500
            System.Net.HttpStatusCode.BadGateway => true,           // 502
            System.Net.HttpStatusCode.ServiceUnavailable => true,   // 503
            System.Net.HttpStatusCode.GatewayTimeout => true,       // 504
            _ => false
        },
        TaskCanceledException => true,    // Timeout
        TimeoutException => true,
        _ => false
    };
}
