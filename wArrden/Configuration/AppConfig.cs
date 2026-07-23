namespace wArrden.Configuration;

public class AppConfig
{
    public string? LogLevel { get; set; }
    public List<InstanceConfig> Instances { get; set; } = new();
    public QueueCleanupRulesConfig? QueueCleanupRules { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public void AddWarning(string message)
    {
        lock (Warnings)
            Warnings.Add(message);
    }

    public void AddValidationError(string message, string? detail = null)
    {
        lock (Errors)
        {
            Errors.Add(message);
            if (detail is not null)
                Errors.Add(detail);
        }
    }

    public void AddValidationWarning(string message, string? detail = null)
    {
        lock (Warnings)
        {
            Warnings.Add(message);
            if (detail is not null)
                Warnings.Add(detail);
        }
    }
}

public class QueueCleanupRulesConfig
{
    public List<QueueCleanupRuleConfig>? Sonarr { get; set; }
    public List<QueueCleanupRuleConfig>? Radarr { get; set; }
    public List<QueueCleanupRuleConfig>? Lidarr { get; set; }
    public List<QueueCleanupRuleConfig>? Whisparr { get; set; }
}

public class QueueCleanupRuleConfig
{
    public string Match { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public class InstanceConfig
{
    public string Type { get; set; } = string.Empty;
    public bool? Enabled { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ApiVersion { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public IndexerFilterConfig? IndexerFilter { get; set; }
    public JobConfig? MissingSearch { get; set; }
    public JobConfig? UpgradeSearch { get; set; }
    public JobConfig? QueueCleanup { get; set; }

    private string? _instanceKey;
    public string InstanceKey => _instanceKey ??= Name.ToLowerInvariant();
    public bool IsSonarr => string.Equals(Type, "sonarr", StringComparison.OrdinalIgnoreCase);
    public bool IsRadarr => string.Equals(Type, "radarr", StringComparison.OrdinalIgnoreCase);
    public bool IsLidarr => string.Equals(Type, "lidarr", StringComparison.OrdinalIgnoreCase);
    public bool IsWhisparr => string.Equals(Type, "whisparr", StringComparison.OrdinalIgnoreCase);
    public bool IsWhisparrV3Eros => IsWhisparr && string.Equals(ApiVersion, "v3-eros", StringComparison.OrdinalIgnoreCase);
}

public class IndexerFilterConfig
{
    public bool? Enabled { get; set; }
    public List<string>? Include { get; set; }
    public List<string>? Exclude { get; set; }
}

public class JobConfig
{
    public bool? Enabled { get; set; }
    public string? Cron { get; set; }
    public int? MaxResults { get; set; }
    public string? Cooldown { get; set; }
    public string? SearchType { get; set; }
    public TaggingConfig? Tagging { get; set; }
}

public class TaggingConfig
{
    public bool? Enabled { get; set; }
    public string? Name { get; set; }
    public bool? Retroactive { get; set; }
}
