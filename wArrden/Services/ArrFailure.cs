using System.Net;
using System.Net.Sockets;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace wArrden.Services;

/// <summary>
/// Classifies exceptions thrown while talking to an arr instance.
/// <para>
/// These failures are the user's environment or configuration — a slow/unreachable
/// instance, a proxy 502, a wrong API key — never a defect in wArrden. They are logged
/// to the console but must not be reported to Beacon (Sentry). Only unexpected exception
/// types (which are genuine wArrden bugs) should be captured.
/// </para>
/// </summary>
public static class ArrFailure
{
    /// <summary>
    /// True when the exception represents an unhealthy arr instance or network rather
    /// than a wArrden bug: transport failures (connection refused, DNS, resets), transient
    /// HTTP status codes surfaced by <c>EnsureSuccessStatusCode</c>, and timeouts.
    /// </summary>
    public static bool IsEnvironmental(Exception ex) => ex is
        HttpRequestException        // non-2xx (5xx, 401/403/404) + connection-refused/reset
        or SocketException
        or TimeoutRejectedException // Polly per-attempt timeout
        or BrokenCircuitException   // Polly circuit open (if a breaker is ever configured)
        or TaskCanceledException
        or OperationCanceledException; // timeout-driven (scheduled jobs pass CancellationToken.None)

    /// <summary>
    /// True when the arr rejected the request for authentication/authorization reasons,
    /// i.e. a wrong, stale, or missing API key. Such an instance is disabled rather than
    /// re-failed every run.
    /// </summary>
    public static bool IsAuthFailure(Exception ex) =>
        ex is HttpRequestException
        {
            StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        };
}
