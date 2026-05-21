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
}
