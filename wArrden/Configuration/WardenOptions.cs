namespace wArrden.Configuration;

public class WardenOptions
{
    public const string Section = "Warden";

    public string? DryRun { get; set; }
    public bool IsDryRun => string.Equals(DryRun, "true", StringComparison.OrdinalIgnoreCase);
    public string? Timezone { get; set; }
    public string? AppVersion { get; set; }
    public string DatabasePath { get; set; } = "data/warden.db";

    // Number of retry attempts for transient arr HTTP failures (5xx, timeouts, connection
    // refused). Optional; defaults to 3 when unset or unparseable. A value of 0 disables retry.
    public const int DefaultHttpRetryCount = 3;
    public string? HttpRetryCount { get; set; }
    public int HttpRetryCountValue =>
        int.TryParse(HttpRetryCount, out var v) && v >= 0 ? v : DefaultHttpRetryCount;

    // Per-attempt HTTP timeout in seconds. Optional; defaults to 30 when unset or unparseable.
    public const int DefaultHttpTimeoutSeconds = 30;
    public string? HttpTimeoutSeconds { get; set; }
    public int HttpTimeoutSecondsValue =>
        int.TryParse(HttpTimeoutSeconds, out var v) && v > 0 ? v : DefaultHttpTimeoutSeconds;
}
