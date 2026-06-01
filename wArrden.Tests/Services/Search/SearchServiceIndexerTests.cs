using wArrden.Clients;
using wArrden.Clients.Models;
using wArrden.Configuration;

namespace wArrden.Tests;

public class SearchServiceIndexerTests : SearchServiceTestBase
{
    private static IndexerFilterConfig FilterWith(params string[] include) =>
        new() { Enabled = true, Include = include.ToList() };

    private static IndexerFilterConfig ExcludeFilterWith(params string[] exclude) =>
        new() { Enabled = true, Exclude = exclude.ToList() };

    private static IndexerFilterConfig FilterWithIncludeAndExclude(string[] include, string[] exclude) =>
        new() { Enabled = true, Include = include.ToList(), Exclude = exclude.ToList() };

    private static IndexerFilterConfig DisabledFilter() =>
        new() { Enabled = false };

    [Fact]
    public async Task SearchMissingEpisodes_IndexerFilterNull_UsesHasAnyEnabledIndexer()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupHasIndexers();
        SetupEpisodeTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Never);
        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerFilterEnabledFalse_UsesHasAnyEnabledIndexer()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupHasIndexers();
        SetupEpisodeTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, DisabledFilter(), null, CancellationToken.None);

        ClientMock.Verify(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Never);
        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerFilterIncludeMatch_ProceedsWithSearch()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true },
            new() { Id = 2, Name = "Rarbg", EnableAutomaticSearch = false }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupEpisodeTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            FilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()), Times.Never);
        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerFilterIncludeNoMatch_SkipsSearch()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "Rarbg", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            FilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerFilterCaseInsensitive_Proceeds()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupEpisodeTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            FilterWith("nzbgeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerNotAutomaticSearch_Skips()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = false }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            FilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_ExcludeOnly_ProceedsWhenOthersExist()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true },
            new() { Id = 2, Name = "DrunkenSlug", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupEpisodeTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            ExcludeFilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_ExcludeOnly_SkipsWhenAllExcluded()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            ExcludeFilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IncludeAndExclude_ExcludeTakesPriority()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true },
            new() { Id = 2, Name = "DrunkenSlug", EnableAutomaticSearch = true },
            new() { Id = 3, Name = "NinjaCentral", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupEpisodeTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            FilterWithIncludeAndExclude(["NZBGeek", "DrunkenSlug", "NinjaCentral"], ["NZBGeek"]), null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingMovies_IndexerFilterIncludeMatch_ProceedsWithSearch()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupMovieTrigger();

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false,
            FilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingMovies_IndexerFilterIncludeNoMatch_SkipsSearch()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "Rarbg", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false,
            FilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchUpgradeMovies_IndexerFilterIncludeMatch_ProceedsWithSearch()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: []);
        SetupMovieTrigger();

        await Service.SearchUpgradeMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false,
            FilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_IndexerFilterIncludeMatch_Proceeds()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: []);
        SetupSeasonTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", false,
            FilterWith("NZBGeek"), null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerFilterNull_DryRun_NoIndexerCheck()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 2, DefaultCooldown, "episode", true, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()), Times.Never);
        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerFilterIncludeNoMatch_EffectiveDetailInWarning()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var indexers = new List<IndexerResource>
        {
            new() { Id = 1, Name = "NZBGeek", EnableAutomaticSearch = true }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.GetIndexersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(indexers);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);

        string? warningDetail = null;
        OutputMock
            .Setup(o => o.WriteWarning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, detail) => warningDetail = detail);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            FilterWith("DrunkenSlug"), null, CancellationToken.None);

        Assert.NotNull(warningDetail);
        Assert.Contains("NZBGeek", warningDetail);
        Assert.Contains("DrunkenSlug", warningDetail);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_NoIndexersAvailable_UsesUpgradeWarningContext()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: []);
        SetupHasIndexers(false);

        string? warningContext = null;
        OutputMock
            .Setup(o => o.WriteWarning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((context, _, _) => warningContext = context);

        await Service.SearchUpgradeEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false,
            null, null, CancellationToken.None);

        Assert.Equal("sonarr.upgrade", warningContext);
    }
}
