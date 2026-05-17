using wArrden.Configuration;

namespace wArrden.Tests;

[Collection("EnvVars")]
public class LegacyConfigLoaderTests
{
    private static void SetEnv(string name, string? value)
    {
        if (value is null)
            Environment.SetEnvironmentVariable(name, null);
        else
            Environment.SetEnvironmentVariable(name, value);
    }

    private static void ClearSonarrEnv()
    {
        SetEnv("SONARR_URL", null);
        SetEnv("SONARR_API_KEY", null);
        SetEnv("SONARR_API_VERSION", null);
        SetEnv("SONARR_QUEUE_CLEANUP_CRON", null);
        SetEnv("SONARR_MISSING_SEARCH_CRON", null);
        SetEnv("SONARR_MISSING_MAX_RESULTS", null);
        SetEnv("SONARR_MISSING_COOLDOWN", null);
        SetEnv("SONARR_UPGRADE_SEARCH_CRON", null);
        SetEnv("SONARR_UPGRADE_MAX_RESULTS", null);
        SetEnv("SONARR_UPGRADE_COOLDOWN", null);
    }

    private static void ClearRadarrEnv()
    {
        SetEnv("RADARR_URL", null);
        SetEnv("RADARR_API_KEY", null);
        SetEnv("RADARR_API_VERSION", null);
        SetEnv("RADARR_QUEUE_CLEANUP_CRON", null);
        SetEnv("RADARR_MISSING_SEARCH_CRON", null);
        SetEnv("RADARR_MISSING_MAX_RESULTS", null);
        SetEnv("RADARR_MISSING_COOLDOWN", null);
        SetEnv("RADARR_UPGRADE_SEARCH_CRON", null);
        SetEnv("RADARR_UPGRADE_MAX_RESULTS", null);
        SetEnv("RADARR_UPGRADE_COOLDOWN", null);
    }

    public LegacyConfigLoaderTests()
    {
        ClearSonarrEnv();
        ClearRadarrEnv();
    }

    [Fact]
    public void LoadFromEnv_NoVars_ReturnsNull()
    {
        var config = LegacyConfigLoader.LoadFromEnv();
        Assert.Null(config);
    }

    [Fact]
    public void LoadFromEnv_SonarrOnly_ReturnsOneInstance()
    {
        SetEnv("SONARR_URL", "http://localhost:8989");
        SetEnv("SONARR_API_KEY", "abc123");

        var config = LegacyConfigLoader.LoadFromEnv();

        Assert.NotNull(config);
        Assert.Single(config.Instances);
        Assert.True(config.Instances[0].IsSonarr);
        Assert.Equal("Sonarr", config.Instances[0].Name);
    }

    [Fact]
    public void LoadFromEnv_RadarrOnly_ReturnsOneInstance()
    {
        SetEnv("RADARR_URL", "http://localhost:7878");
        SetEnv("RADARR_API_KEY", "abc123");

        var config = LegacyConfigLoader.LoadFromEnv();

        Assert.NotNull(config);
        Assert.Single(config.Instances);
        Assert.True(config.Instances[0].IsRadarr);
        Assert.Equal("Radarr", config.Instances[0].Name);
    }

    [Fact]
    public void LoadFromEnv_Both_ReturnsTwoInstances()
    {
        SetEnv("SONARR_URL", "http://localhost:8989");
        SetEnv("SONARR_API_KEY", "abc123");
        SetEnv("RADARR_URL", "http://localhost:7878");
        SetEnv("RADARR_API_KEY", "xyz789");

        var config = LegacyConfigLoader.LoadFromEnv();

        Assert.NotNull(config);
        Assert.Equal(2, config.Instances.Count);
        Assert.Contains(config.Instances, i => i.IsSonarr);
        Assert.Contains(config.Instances, i => i.IsRadarr);
    }

    [Fact]
    public void LoadFromEnv_MissingApiKey_SkipsInstance()
    {
        SetEnv("SONARR_URL", "http://localhost:8989");

        var config = LegacyConfigLoader.LoadFromEnv();

        Assert.Null(config);
    }

    [Fact]
    public void LoadFromEnv_JobWithCron_EnabledTrue()
    {
        SetEnv("SONARR_URL", "http://localhost:8989");
        SetEnv("SONARR_API_KEY", "abc123");
        SetEnv("SONARR_QUEUE_CLEANUP_CRON", "*/5 * * * *");

        var config = LegacyConfigLoader.LoadFromEnv();

        Assert.NotNull(config);
        var job = config.Instances[0].QueueCleanup;
        Assert.NotNull(job);
        Assert.True(job.Enabled);
        Assert.Equal("*/5 * * * *", job.Cron);
    }

    [Fact]
    public void LoadFromEnv_OptionalFields_ParsedCorrectly()
    {
        SetEnv("SONARR_URL", "http://localhost:8989");
        SetEnv("SONARR_API_KEY", "abc123");
        SetEnv("SONARR_API_VERSION", "3");
        SetEnv("SONARR_MISSING_SEARCH_CRON", "0 */6 * * *");
        SetEnv("SONARR_MISSING_MAX_RESULTS", "25");
        SetEnv("SONARR_MISSING_COOLDOWN", "7d");

        var config = LegacyConfigLoader.LoadFromEnv();

        Assert.NotNull(config);
        var inst = config.Instances[0];
        Assert.Equal("3", inst.ApiVersion);
        Assert.NotNull(inst.MissingSearch);
        Assert.True(inst.MissingSearch.Enabled);
        Assert.Equal("0 */6 * * *", inst.MissingSearch.Cron);
        Assert.Equal(25, inst.MissingSearch.MaxResults);
        Assert.Equal("7d", inst.MissingSearch.Cooldown);
    }

    [Fact]
    public void LoadFromEnv_NoCron_JobDisabled()
    {
        SetEnv("SONARR_URL", "http://localhost:8989");
        SetEnv("SONARR_API_KEY", "abc123");

        var config = LegacyConfigLoader.LoadFromEnv();

        Assert.NotNull(config);
        Assert.Null(config.Instances[0].MissingSearch);
        Assert.Null(config.Instances[0].UpgradeSearch);
        Assert.Null(config.Instances[0].QueueCleanup);
    }

    [Fact]
    public void LoadFromEnv_DeprecationWarningPrinted()
    {
        ResetWarnedFlag();

        var errorWriter = new StringWriter();
        var originalError = Console.Error;
        Console.SetError(errorWriter);

        try
        {
            SetEnv("SONARR_URL", "http://localhost:8989");
            SetEnv("SONARR_API_KEY", "abc123");

            var config = LegacyConfigLoader.LoadFromEnv();

            Assert.NotNull(config);
            var output = errorWriter.ToString();
            Assert.Contains("deprecated", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    private static void ResetWarnedFlag()
    {
        var field = typeof(LegacyConfigLoader).GetField("_warned",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field?.SetValue(null, false);
    }
}
