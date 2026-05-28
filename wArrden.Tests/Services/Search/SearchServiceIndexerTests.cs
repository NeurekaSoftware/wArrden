using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class SearchServiceIndexerTests : SearchServiceTestBase
{
    [Fact]
    public async Task SearchMissingEpisodes_IndexerNamesNull_UsesHasAnyEnabledIndexer()
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
    public async Task SearchMissingEpisodes_IndexerNamesEmpty_UsesHasAnyEnabledIndexer()
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

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, new List<string>(), null, CancellationToken.None);

        ClientMock.Verify(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerNamesMatch_ProceedsWithSearch()
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
            new List<string> { "NZBGeek" }, null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()), Times.Never);
        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerNamesNoMatch_SkipsSearch()
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
            new List<string> { "NZBGeek" }, null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerNamesCaseInsensitive_Proceeds()
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
            new List<string> { "nzbgeek" }, null, CancellationToken.None);

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
            new List<string> { "NZBGeek" }, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchMissingMovies_IndexerNamesMatch_ProceedsWithSearch()
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
            new List<string> { "NZBGeek" }, null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingMovies_IndexerNamesNoMatch_SkipsSearch()
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
            new List<string> { "NZBGeek" }, null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchUpgradeMovies_IndexerNamesMatch_ProceedsWithSearch()
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
            new List<string> { "NZBGeek" }, null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_IndexerNamesMatch_Proceeds()
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
            new List<string> { "NZBGeek" }, null, CancellationToken.None);

        ClientMock.Verify(c => c.GetIndexersAsync(It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_IndexerNamesNull_DryRun_NoIndexerCheck()
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
}
