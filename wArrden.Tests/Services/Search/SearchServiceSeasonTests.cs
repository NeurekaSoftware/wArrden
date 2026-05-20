using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class SearchServiceSeasonTests : SearchServiceTestBase
{
    [Fact]
    public async Task SearchMissingEpisodes_Season_FullHappyPath_TriggersAndMarksCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 2, SeriesId = 100, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 3, SeriesId = 200, Title = "Ep1", SeasonNumber = 2, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Another", Year = 2021 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: []);
        SetupHasIndexers();
        SetupSeasonTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(200, 2, It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
        CooldownMock.Verify(c => c.CleanExpiredAsync("Sonarr", "Missing_Season", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.GetCooldownIdsAsync("Sonarr", "Missing_Season", It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing_Season", It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_Season_FullHappyPath_TriggersAndMarksCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade_Season");
        SetupCooldownIds(category: "Upgrade_Season", ids: []);
        SetupHasIndexers();
        SetupSeasonTrigger();

        await Service.SearchUpgradeEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Upgrade_Season", It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_DryRun_DoesNotTriggerSearch()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: []);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", true, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_NoWantedItems_ShowsZeroStats()
    {
        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WantedEpisodeResource>());

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 2, DefaultCooldown, "season", false, null, CancellationToken.None);

        CooldownMock.Verify(
            c => c.CleanExpiredAsync("Sonarr", "Missing_Season", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
        ClientMock.Verify(
            c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_AllOnCooldown_NoSearchTriggered()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 },
            new() { Id = 2, SeriesId = 200, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 1 }
        };

        var seasonKey1 = 100 * 1000 + 1;
        var seasonKey2 = 200 * 1000 + 1;

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: [seasonKey1, seasonKey2]);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_SomeOnCooldown_CorrectStats()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 2, SeriesId = 200, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Another", Year = 2021 } }
        };

        var seasonKey1 = 100 * 1000 + 1;

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: [seasonKey1]);
        SetupHasIndexers();
        SetupSeasonTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(200, 1, It.IsAny<CancellationToken>()), Times.Once);
        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_IndependentFromEpisodeCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: []);
        SetupHasIndexers();
        SetupSeasonTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", false, null, CancellationToken.None);

        CooldownMock.Verify(c => c.CleanExpiredAsync("Sonarr", "Missing_Season", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.GetCooldownIdsAsync("Sonarr", "Missing_Season", It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.CleanExpiredAsync("Sonarr", "Missing", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_MaxResultsLimitsSeasons()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 2, SeriesId = 200, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Another", Year = 2021 } },
            new() { Id = 3, SeriesId = 300, Title = "Ep3", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Third", Year = 2022 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: []);
        SetupHasIndexers();
        SetupSeasonTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 2, DefaultCooldown, "season", false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SearchMissingEpisodes_Season_NoIndexersAvailable_SkipsSearchAndCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Missing_Season");
        SetupCooldownIds(category: "Missing_Season", ids: []);
        SetupHasIndexers(false);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "season", false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing_Season",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
