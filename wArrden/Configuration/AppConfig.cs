namespace wArrden.Configuration;

public class AppConfig
{
    public List<InstanceConfig> Instances { get; set; } = new();
}

public class InstanceConfig
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "3";
    public JobConfig? MissingSearch { get; set; }
    public JobConfig? UpgradeSearch { get; set; }
    public JobConfig? QueueCleanup { get; set; }

    public string InstanceKey => Name.ToLowerInvariant();
    public bool IsSonarr => string.Equals(Type, "sonarr", StringComparison.OrdinalIgnoreCase);
    public bool IsRadarr => string.Equals(Type, "radarr", StringComparison.OrdinalIgnoreCase);
}

public class JobConfig
{
    public bool Enabled { get; set; }
    public string Cron { get; set; } = string.Empty;
    public int MaxResults { get; set; }
    public string Cooldown { get; set; } = "30d";
    public string? SearchType { get; set; }
}
