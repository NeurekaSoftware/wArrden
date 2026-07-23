using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace wArrden.Clients.Http;

/// <summary>
/// Shared HTTP resilience for every arr client. Transient failures (5xx such as 502,
/// timeouts, connection refused) are retried with exponential backoff so brief blips recover
/// silently; each attempt is bounded by its own timeout. Non-transient responses — notably
/// 401/403 (bad API key) and 4xx — are never retried, so a misconfiguration fails fast.
/// <para>
/// Kept as an extension so production (per-instance named clients) and tests configure the
/// pipeline identically.
/// </para>
/// </summary>
public static class ArrResilience
{
    public static IHttpClientBuilder AddArrResilience(
        this IHttpClientBuilder builder, int retryCount, TimeSpan attemptTimeout, TimeSpan? baseDelay = null)
    {
        builder.AddResilienceHandler("arr", pipeline =>
        {
            // Retry is the outer strategy so each attempt gets a fresh timeout below.
            // The default ShouldHandle treats transient HTTP status codes, HttpRequestException,
            // and per-attempt timeouts as retryable — and leaves 401/403/4xx alone.
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = retryCount,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = baseDelay ?? TimeSpan.FromSeconds(1)
            });

            // Per-attempt timeout (inner). The HttpClient.Timeout is left infinite so it never
            // cuts the retry sequence short; this strategy bounds each individual attempt.
            pipeline.AddTimeout(attemptTimeout);
        });

        return builder;
    }
}
