using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Configuration;

namespace RetireRight.Scrapers.AntiBot;

public class RequestThrottler
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestWindows = new();
    private readonly Random _rng = new();
    private readonly ILogger<RequestThrottler> _logger;

    public RequestThrottler(ILogger<RequestThrottler> logger)
    {
        _logger = logger;
    }

    public async Task ThrottleAsync(string sourceName, RateLimitOptions options, CancellationToken ct = default)
    {
        var semaphore = _semaphores.GetOrAdd(sourceName, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            // Check sliding window rate limit
            var window = _requestWindows.GetOrAdd(sourceName, _ => new Queue<DateTime>());
            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-1);

            // Remove expired entries
            while (window.Count > 0 && window.Peek() < windowStart)
                window.Dequeue();

            // If at limit, wait until oldest request expires
            if (window.Count >= options.MaxRequestsPerMinute)
            {
                var waitUntil = window.Peek().AddMinutes(1);
                var delay = waitUntil - now;
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug("Rate limit reached for {Source}, waiting {Delay}ms", sourceName, delay.TotalMilliseconds);
                    await Task.Delay(delay, ct);
                }
            }

            // Add random human-like delay
            var randomDelay = _rng.Next(options.MinDelayMs, options.MaxDelayMs);
            _logger.LogDebug("Throttling {Source}: {Delay}ms delay", sourceName, randomDelay);
            await Task.Delay(randomDelay, ct);

            // Record this request
            window.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
