namespace wArrden.Configuration;

public class AppConfig
{
    public string? LogLevel { get; set; }
    public List<InstanceConfig> Instances { get; set; } = new();
    public QueueCleanupRulesConfig? QueueCleanupRules { get; set; }
    public List<string> Warnings { get; set; } = new();
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
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "3";
    public List<string>? IndexerNames { get; set; }
    public JobConfig? MissingSearch { get; set; }
    public JobConfig? UpgradeSearch { get; set; }
    public JobConfig? QueueCleanup { get; set; }

    private string? _instanceKey;
    public string InstanceKey => _instanceKey ??= Name.ToLowerInvariant();
    public bool IsSonarr => string.Equals(Type, "sonarr", StringComparison.OrdinalIgnoreCase);
    public bool IsRadarr => string.Equals(Type, "radarr", StringComparison.OrdinalIgnoreCase);
    public bool IsLidarr => string.Equals(Type, "lidarr", StringComparison.OrdinalIgnoreCase);
    public bool IsWhisparr => string.Equals(Type, "whisparr", StringComparison.OrdinalIgnoreCase);
}

public class JobConfig
{
    public bool? Enabled { get; set; }
    public string? Cron { get; set; }
    public int? MaxResults { get; set; }
    public string? Cooldown { get; set; }
    public string? SearchType { get; set; }
}
