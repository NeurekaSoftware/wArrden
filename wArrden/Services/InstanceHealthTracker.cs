using System.Collections.Concurrent;

namespace wArrden.Services;

/// <summary>
/// Outcome of validating an instance's connectivity and API key at startup.
/// </summary>
public enum ValidationStatus
{
    /// <summary>The instance is reachable and the API key is accepted.</summary>
    Ok,

    /// <summary>The instance is reachable but rejected the API key (401/403) — a misconfiguration.</summary>
    AuthFailed,

    /// <summary>The instance could not be reached (connection refused, timeout, DNS, etc.).</summary>
    Unreachable
}

/// <summary>
/// Tracks which instances have been disabled at runtime. An instance is disabled when its
/// API key is rejected — at startup, or mid-run if the key is rotated — so its scheduled jobs
/// stop running instead of re-failing (and re-reporting) every cycle. Disabled state lasts
/// until the process restarts, at which point the operator's corrected config is re-validated.
/// </summary>
public sealed class InstanceHealthTracker
{
    private readonly ConcurrentDictionary<string, string> _disabled = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the instance's scheduled jobs may run.</summary>
    public bool IsEnabled(string instanceKey) => !_disabled.ContainsKey(instanceKey);

    /// <summary>
    /// Disables the instance. Returns <c>true</c> only the first time an instance is disabled,
    /// so callers can log/report exactly once and avoid flooding on repeated failures.
    /// </summary>
    public bool Disable(string instanceKey, string reason) => _disabled.TryAdd(instanceKey, reason);
}
