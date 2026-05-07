namespace ArrWarden.Configuration;

public class WardenOptions
{
    public const string Section = "Warden";

    public string? DryRun { get; set; }
    public bool IsDryRun => string.Equals(DryRun, "true", StringComparison.OrdinalIgnoreCase);
    public string DatabasePath { get; set; } = "data/warden.db";

    public string? SonarrUrl { get; set; }
    public string? SonarrApiKey { get; set; }
    public string SonarrApiVersion { get; set; } = "3";
    public string? SonarrQueueCleanupCron { get; set; }
    public string? SonarrMissingSearchCron { get; set; }
    public string SonarrMissingCooldownRaw { get; set; } = "30d";
    public TimeSpan SonarrMissingCooldown => DurationParser.Parse(SonarrMissingCooldownRaw);
    public int SonarrMissingMaxResults { get; set; } = 100;
    public string? SonarrUpgradeSearchCron { get; set; }
    public string SonarrUpgradeCooldownRaw { get; set; } = "30d";
    public TimeSpan SonarrUpgradeCooldown => DurationParser.Parse(SonarrUpgradeCooldownRaw);
    public int SonarrUpgradeMaxResults { get; set; } = 50;

    public string? RadarrUrl { get; set; }
    public string? RadarrApiKey { get; set; }
    public string RadarrApiVersion { get; set; } = "3";
    public string? RadarrQueueCleanupCron { get; set; }
    public string? RadarrMissingSearchCron { get; set; }
    public string RadarrMissingCooldownRaw { get; set; } = "30d";
    public TimeSpan RadarrMissingCooldown => DurationParser.Parse(RadarrMissingCooldownRaw);
    public int RadarrMissingMaxResults { get; set; } = 100;
    public string? RadarrUpgradeSearchCron { get; set; }
    public string RadarrUpgradeCooldownRaw { get; set; } = "30d";
    public TimeSpan RadarrUpgradeCooldown => DurationParser.Parse(RadarrUpgradeCooldownRaw);
    public int RadarrUpgradeMaxResults { get; set; } = 50;

    public Dictionary<string, bool> SonarrBlacklistRules { get; set; } = new();
    public Dictionary<string, bool> RadarrBlacklistRules { get; set; } = new();

    public bool HasSonarr => !string.IsNullOrWhiteSpace(SonarrUrl) && !string.IsNullOrWhiteSpace(SonarrApiKey);
    public bool HasRadarr => !string.IsNullOrWhiteSpace(RadarrUrl) && !string.IsNullOrWhiteSpace(RadarrApiKey);
}
