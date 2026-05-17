namespace wArrden.Configuration;

internal static class LegacyConfigLoader
{
    private static bool _warned;

    public static AppConfig? LoadFromEnv()
    {
        var sonarrUrl = GetEnv("SONARR_URL");
        var sonarrKey = GetEnv("SONARR_API_KEY");
        var radarrUrl = GetEnv("RADARR_URL");
        var radarrKey = GetEnv("RADARR_API_KEY");

        var hasSonarr = !string.IsNullOrWhiteSpace(sonarrUrl) && !string.IsNullOrWhiteSpace(sonarrKey);
        var hasRadarr = !string.IsNullOrWhiteSpace(radarrUrl) && !string.IsNullOrWhiteSpace(radarrKey);

        if (!hasSonarr && !hasRadarr)
            return null;

        if (!_warned)
        {
            Console.Error.WriteLine("Warning: Environment variable configuration is deprecated.");
            Console.Error.WriteLine("         Please migrate to config.yaml. See config.example.yaml for the new format.");
            _warned = true;
        }

        var config = new AppConfig();

        if (hasSonarr)
            config.Instances.Add(BuildSonarrConfig());

        if (hasRadarr)
            config.Instances.Add(BuildRadarrConfig());

        return config;
    }

    private static InstanceConfig BuildSonarrConfig()
    {
        return new InstanceConfig
        {
            Type = "sonarr",
            Name = "Sonarr",
            Url = GetEnv("SONARR_URL")!,
            ApiKey = GetEnv("SONARR_API_KEY")!,
            ApiVersion = GetEnv("SONARR_API_VERSION") ?? "3",
            QueueCleanup = BuildJob("SONARR_QUEUE_CLEANUP_CRON"),
            MissingSearch = BuildJob("SONARR_MISSING_SEARCH_CRON",
                maxResults: int.TryParse(GetEnv("SONARR_MISSING_MAX_RESULTS"), out var smr) ? smr : 100,
                cooldown: GetEnv("SONARR_MISSING_COOLDOWN")),
            UpgradeSearch = BuildJob("SONARR_UPGRADE_SEARCH_CRON",
                maxResults: int.TryParse(GetEnv("SONARR_UPGRADE_MAX_RESULTS"), out var sur) ? sur : 50,
                cooldown: GetEnv("SONARR_UPGRADE_COOLDOWN"))
        };
    }

    private static InstanceConfig BuildRadarrConfig()
    {
        return new InstanceConfig
        {
            Type = "radarr",
            Name = "Radarr",
            Url = GetEnv("RADARR_URL")!,
            ApiKey = GetEnv("RADARR_API_KEY")!,
            ApiVersion = GetEnv("RADARR_API_VERSION") ?? "3",
            QueueCleanup = BuildJob("RADARR_QUEUE_CLEANUP_CRON"),
            MissingSearch = BuildJob("RADARR_MISSING_SEARCH_CRON",
                maxResults: int.TryParse(GetEnv("RADARR_MISSING_MAX_RESULTS"), out var rmr) ? rmr : 100,
                cooldown: GetEnv("RADARR_MISSING_COOLDOWN")),
            UpgradeSearch = BuildJob("RADARR_UPGRADE_SEARCH_CRON",
                maxResults: int.TryParse(GetEnv("RADARR_UPGRADE_MAX_RESULTS"), out var rur) ? rur : 50,
                cooldown: GetEnv("RADARR_UPGRADE_COOLDOWN"))
        };
    }

    private static JobConfig? BuildJob(string cronEnv, int? maxResults = null, string? cooldown = null)
    {
        var cron = GetEnv(cronEnv);
        if (string.IsNullOrWhiteSpace(cron)) return null;

        return new JobConfig
        {
            Enabled = true,
            Cron = cron,
            MaxResults = maxResults ?? 0,
            Cooldown = cooldown ?? "30d"
        };
    }

    private static string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);
}
