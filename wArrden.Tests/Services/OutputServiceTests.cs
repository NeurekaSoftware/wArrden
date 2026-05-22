using wArrden.Configuration;
using wArrden.Services;

namespace wArrden.Tests;

public class OutputServiceTests
{
    private readonly StringWriter _writer;
    private readonly OutputService _output;

    public OutputServiceTests()
    {
        _writer = new StringWriter();
        _output = new OutputService { Out = _writer };
    }

    [Fact]
    public void WriteBanner_ContainsStartupAndReady()
    {
        var config = new AppConfig();
        var opts = new WardenOptions();

        OutputService.WriteBanner(config, opts, _writer);

        var output = _writer.ToString();
        Assert.Contains("wArrden", output);
        Assert.Contains("system.startup", output);
        Assert.Contains("system.ready", output);
    }

    [Fact]
    public void WriteBanner_WithInstances_ShowsInstanceNames()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr", Name = "Series", Url = "http://localhost:8989",
                    ApiKey = "key", QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var opts = new WardenOptions();

        OutputService.WriteBanner(config, opts, _writer);

        var output = _writer.ToString();
        Assert.Contains("Series", output);
        Assert.Contains("http://localhost:8989", output);
        Assert.Contains("Queue Cleanup", output);
    }

    [Fact]
    public void WriteBanner_ShowsRuntimeSection()
    {
        var config = new AppConfig();
        var opts = new WardenOptions { DryRun = "true" };

        OutputService.WriteBanner(config, opts, _writer);

        var output = _writer.ToString();
        Assert.Contains("Runtime", output);
        Assert.Contains("Timezone", output);
        Assert.Contains("Local Time", output);
        Assert.Contains("UTC Offset", output);
        Assert.Contains("Dry Run", output);
        Assert.Contains("true", output);
    }

    [Fact]
    public void WriteQueueResult_NoMatches_ShowsNoBlocked()
    {
        _output.WriteQueueResult(DateTime.Now, "TestSonarr", 150, 0, 0,
            Array.Empty<(string, string)>(), false);

        var output = _writer.ToString();
        Assert.Contains("Total Queue", output);
        Assert.Contains("150", output);
        Assert.Contains("No blocked queue items detected", output);
    }

    [Fact]
    public void WriteQueueResult_WithMatches_Live()
    {
        var items = new List<(string, string)>
        {
            ("The Boys (2019) - S01E01", "NOT_AN_UPGRADE")
        };

        _output.WriteQueueResult(DateTime.Now, "TestSonarr", 150, 5, 1,
            items, false);

        var output = _writer.ToString();
        Assert.Contains("Blocked", output);
        Assert.Contains("5", output);
        Assert.Contains("Matched", output);
        Assert.Contains("Blocklisted 1", output);
        Assert.Contains("The Boys", output);
        Assert.Contains("NOT_AN_UPGRADE", output);
    }

    [Fact]
    public void WriteQueueResult_WithMatches_DryRun()
    {
        var items = new List<(string, string)>
        {
            ("Test Item", "SAMPLE")
        };

        _output.WriteQueueResult(DateTime.Now, "TestSonarr", 150, 2, 1,
            items, true);

        var output = _writer.ToString();
        Assert.Contains("Would blocklist 1", output);
    }

    [Fact]
    public void WriteQueueResult_Overflow_ShowsPlusMore()
    {
        var items = new List<(string, string)>(); // empty items, but matched > 0

        _output.WriteQueueResult(DateTime.Now, "TestSonarr", 150, 10, 5,
            items, false);

        var output = _writer.ToString();
        Assert.Contains("+5 more", output);
    }

    [Theory]
    [InlineData("Missing Search", "sonarr.missing")]
    [InlineData("Upgrade Search", "sonarr.upgrade")]
    [InlineData("Queue Cleanup", "sonarr.queue")]
    public void InstanceJobLabel_MapsCorrectly(string job, string expectedSuffix)
    {
        var writer = new OutputService.SearchOutputWriter(
            "Sonarr", job, 10, _writer);
        writer.WriteHeader();

        var output = _writer.ToString();
        Assert.Contains($"sonarr.{expectedSuffix.Split('.')[1]}", output);
    }

    [Fact]
    public void WriteStats_NoWantedItems_ShowsCorrectResult()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.WriteStats(0, 0, 0, 0, true);

        var output = _writer.ToString();
        Assert.Contains("No wanted items found", output);
    }

    [Fact]
    public void WriteStats_NoSearchPerformed_ShowsCorrectResult()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.WriteStats(5, 5, 0, 0, true);

        var output = _writer.ToString();
        Assert.Contains("No search performed", output);
    }

    [Fact]
    public void WriteStats_SearchedItems_ShowsCorrectResult()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.WriteStats(8, 2, 6, 3, false);

        var output = _writer.ToString();
        Assert.Contains("Searched 3", output);
    }

    [Fact]
    public void WriteStats_IsLast_UsesFinalTreeConnector()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.WriteStats(5, 0, 5, 3, true);

        var output = _writer.ToString();
        Assert.Contains("└─ Stats:", output);
    }

    [Fact]
    public void WriteStats_NotLast_UsesIntermediateTreeConnector()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.WriteStats(5, 0, 5, 3, false);

        var output = _writer.ToString();
        Assert.Contains("├─ Stats:", output);
    }

    [Fact]
    public void WriteBanner_SonarrSearchJobs_ShowsSearchType()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr", Name = "Series", Url = "http://localhost:8989",
                    ApiKey = "key",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", SearchType = "episode" },
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/10 * * * *", SearchType = "season" }
                }
            }
        };
        var opts = new WardenOptions();

        OutputService.WriteBanner(config, opts, _writer);

        var output = _writer.ToString();
        Assert.Contains("(episode)", output);
        Assert.Contains("(season)", output);
    }

    [Fact]
    public void WriteBanner_RadarrSearchJobs_DoesNotShowSearchType()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "radarr", Name = "Movies", Url = "http://localhost:7878",
                    ApiKey = "key",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", SearchType = "episode" }
                }
            }
        };
        var opts = new WardenOptions();

        OutputService.WriteBanner(config, opts, _writer);

        var output = _writer.ToString();
        Assert.DoesNotContain("(episode)", output);
    }

    [Fact]
    public void SearchOutputWriter_SetPhase_WritesPhaseLine()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.SetPhase("Fetching wanted episodes");

        var output = _writer.ToString();
        Assert.Contains("├─ Fetching wanted episodes", output);
    }

    [Fact]
    public void SearchOutputWriter_WriteItem_WritesFormattedItem()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.StartResults();
        writer.WriteItem("The Boys (2019) - S01E01 - The Name of the Game");

        var output = _writer.ToString();
        Assert.Contains("• The Boys (2019) - S01E01 - The Name of the Game", output);
    }

    [Fact]
    public void SearchOutputWriter_StartResults_WritesResultsHeader()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.StartResults();

        var output = _writer.ToString();
        Assert.Contains("└─ Results:", output);
    }

    [Fact]
    public void SearchOutputWriter_WriteTrailer_WritesTrailerLine()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer);
        writer.WriteTrailer();

        var output = _writer.ToString();
        Assert.NotEmpty(output);
    }

    [Fact]
    public void WriteBanner_WhisparrSearchJobs_ShowsSearchType()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr", Name = "Adult", Url = "http://localhost:6969",
                    ApiKey = "key",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", SearchType = "episode" },
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/10 * * * *", SearchType = "season" }
                }
            }
        };
        var opts = new WardenOptions();

        OutputService.WriteBanner(config, opts, _writer);

        var output = _writer.ToString();
        Assert.Contains("(episode)", output);
        Assert.Contains("(season)", output);
    }

    [Fact]
    public void WriteBanner_LidarrSearchJobs_ShowsSearchType()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "lidarr", Name = "Music", Url = "http://localhost:8686",
                    ApiKey = "key", ApiVersion = "1",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", SearchType = "album" },
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/10 * * * *", SearchType = "artist" }
                }
            }
        };
        var opts = new WardenOptions();

        OutputService.WriteBanner(config, opts, _writer);

        var output = _writer.ToString();
        Assert.Contains("(album)", output);
        Assert.Contains("(artist)", output);
    }

    [Fact]
    public void WriteDebug_MessageOnly_WritesFormattedDebugLine()
    {
        _output.MinimumLevel = LogLevel.Debug;
        _output.WriteDebug("warden.config", "Loaded config");

        var output = _writer.ToString();
        Assert.Contains("DBG", output);
        Assert.Contains("warden.config", output);
        Assert.Contains("└─ Loaded config", output);
    }

    [Fact]
    public void WriteDebug_WithDetail_WritesTreeStructure()
    {
        _output.MinimumLevel = LogLevel.Debug;
        _output.WriteDebug("series.missing", "Fetched 45 episodes", "12 on cooldown, 33 eligible");

        var output = _writer.ToString();
        Assert.Contains("DBG", output);
        Assert.Contains("series.missing", output);
        Assert.Contains("├─ Fetched 45 episodes", output);
        Assert.Contains("└─ 12 on cooldown, 33 eligible", output);
    }

    [Fact]
    public void WriteWarning_MessageOnly_WritesToError()
    {
        var errorWriter = new StringWriter();
        _output.Error = errorWriter;

        _output.WriteWarning("series.missing", "No enabled indexers");

        var error = errorWriter.ToString();
        Assert.Contains("WRN", error);
        Assert.Contains("series.missing", error);
        Assert.Contains("└─ No enabled indexers", error);
    }

    [Fact]
    public void WriteWarning_WithDetail_WritesToErrorTree()
    {
        var errorWriter = new StringWriter();
        _output.Error = errorWriter;

        _output.WriteWarning("series.missing", "Search trigger failed", "HttpRequestException: Connection refused");

        var error = errorWriter.ToString();
        Assert.Contains("WRN", error);
        Assert.Contains("series.missing", error);
        Assert.Contains("├─ Search trigger failed", error);
        Assert.Contains("└─ HttpRequestException: Connection refused", error);
    }

    [Fact]
    public void WriteError_MessageOnly_WritesToError()
    {
        var errorWriter = new StringWriter();
        _output.Error = errorWriter;

        _output.WriteError("warden.scheduler", "Task failed");

        var error = errorWriter.ToString();
        Assert.Contains("ERR", error);
        Assert.Contains("warden.scheduler", error);
        Assert.Contains("└─ Task failed", error);
    }

    [Fact]
    public void WriteError_WithException_WritesExceptionDetail()
    {
        var errorWriter = new StringWriter();
        _output.Error = errorWriter;
        var ex = new InvalidOperationException("Unknown instance type: foo");

        _output.WriteError("warden.scheduler", "Scheduled task error", ex);

        var error = errorWriter.ToString();
        Assert.Contains("ERR", error);
        Assert.Contains("warden.scheduler", error);
        Assert.Contains("├─ Scheduled task error", error);
        Assert.Contains("└─ InvalidOperationException: Unknown instance type: foo", error);
    }

    [Fact]
    public void WriteDebug_Suppressed_WhenMinimumIsInfo()
    {
        _output.MinimumLevel = LogLevel.Info;
        _output.WriteDebug("warden.core", "Should not appear");

        var output = _writer.ToString();
        Assert.Empty(output);
    }

    [Fact]
    public void WriteDebug_Shown_WhenMinimumIsDebug()
    {
        _output.MinimumLevel = LogLevel.Debug;
        _output.WriteDebug("warden.core", "Should appear");

        var output = _writer.ToString();
        Assert.Contains("DBG", output);
    }

    [Fact]
    public void WriteWarning_Suppressed_WhenMinimumIsError()
    {
        var errorWriter = new StringWriter();
        _output.Error = errorWriter;
        _output.MinimumLevel = LogLevel.Error;

        _output.WriteWarning("warden.core", "Should not appear");

        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public void WriteError_Shown_WhenMinimumIsError()
    {
        var errorWriter = new StringWriter();
        _output.Error = errorWriter;
        _output.MinimumLevel = LogLevel.Error;

        _output.WriteError("warden.core", "Should appear");

        Assert.Contains("ERR", errorWriter.ToString());
    }

    [Fact]
    public void WriteQueueResult_Suppressed_WhenMinimumIsWarning()
    {
        _output.MinimumLevel = LogLevel.Warning;
        _output.WriteQueueResult(DateTime.Now, "TestSonarr", 150, 0, 0,
            Array.Empty<(string, string)>(), false);

        var output = _writer.ToString();
        Assert.Empty(output);
    }

    [Fact]
    public void WriteQueueResult_Shown_WhenMinimumIsInfo()
    {
        _output.MinimumLevel = LogLevel.Info;
        _output.WriteQueueResult(DateTime.Now, "TestSonarr", 150, 0, 0,
            Array.Empty<(string, string)>(), false);

        var output = _writer.ToString();
        Assert.Contains("INF", output);
    }

    [Fact]
    public void SearchOutputWriter_Suppressed_WhenShouldLogIsFalse()
    {
        var writer = new OutputService.SearchOutputWriter(
            "test", "Missing Search", 10, _writer, shouldLog: false);
        writer.WriteHeader();
        writer.SetPhase("Testing");
        writer.WriteStats(5, 0, 5, 3, true);
        writer.StartResults();
        writer.WriteItem("Test Item");
        writer.WriteTrailer();

        var output = _writer.ToString();
        Assert.Empty(output);
    }
}
