using wArrden.Services;

namespace wArrden.Invocables;

/// <summary>
/// The single failure-reporting policy shared by every scheduled job. It keeps the user's
/// environment out of Beacon:
/// <list type="number">
///   <item>Auth failure (401/403) — disable the instance and log once; never report to Beacon.</item>
///   <item>Other environmental failure (5xx, timeout, connection refused) — log a console
///         warning; never report to Beacon.</item>
///   <item>Anything else (a genuine wArrden defect) — log an error and capture to Beacon.</item>
/// </list>
/// </summary>
internal static class JobFailure
{
    public static void Report(
        OutputService output,
        InstanceHealthTracker health,
        string instanceKey,
        string instanceName,
        string context,
        string message,
        Exception ex)
    {
        var detail = $"{ex.GetType().Name}: {ex.Message}";

        if (ArrFailure.IsAuthFailure(ex))
        {
            // Disable so scheduled jobs stop running; log once (Disable is first-write-wins).
            var code = (ex as HttpRequestException)?.StatusCode;
            var reason = code is not null ? $"API key rejected ({(int)code} {code})" : "API key rejected";
            if (health.Disable(instanceKey, reason))
            {
                output.WriteWarning(context,
                    $"Instance {instanceName} disabled — {reason}; fix the API key and restart wArrden",
                    detail);
            }
            return;
        }

        if (ArrFailure.IsEnvironmental(ex))
        {
            output.WriteWarning(context, message, detail);
            return;
        }

        output.WriteError(context, message, ex);
    }
}
