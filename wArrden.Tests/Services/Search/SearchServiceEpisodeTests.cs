using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class SearchServiceEpisodeTests : SearchServiceTestBase
{
    [Fact]
    public async Task SearchMissingEpisodes_NoWantedItems_ShowsZeroStats()
    {
        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WantedEpisodeResource>());

        SetupOutputCallback();
        SetupCleanExpired();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 2, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        CooldownMock.Verify(
            c => c.CleanExpiredAsync("Sonarr", "Missing", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
        CooldownMock.Verify(
            c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        ClientMock.Verify(
            c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        ClientMock.Verify(
            c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_DryRun_DoesNotTriggerSearch()
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

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_DryRun_DoesNotTriggerSearch()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 }
        };

        ClientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: []);

        await Service.SearchUpgradeEpisodesAsync(ClientMock.Object, 2, DefaultCooldown, "episode", true, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_FullHappyPath_TriggersAndMarksCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 2, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2,
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

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_TriggerFailure_DoesNotMarkCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupHasIndexers();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_AllOnCooldown_NoSearchTriggered()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 },
            new() { Id = 2, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2 }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: [1, 2]);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_AllOnCooldown_NoSearchTriggered()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 },
            new() { Id = 2, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2 }
        };

        ClientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: [1, 2]);

        await Service.SearchUpgradeEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SomeOnCooldown_CorrectStats()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 },
            new() { Id = 2, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2 },
            new() { Id = 3, Title = "Ep3", SeasonNumber = 1, EpisodeNumber = 3 }
        };

        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: [3]);
        SetupHasIndexers();
        SetupEpisodeTrigger();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_SomeOnCooldown_CorrectStats()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 },
            new() { Id = 2, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2 },
            new() { Id = 3, Title = "Ep3", SeasonNumber = 1, EpisodeNumber = 3 }
        };

        ClientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: [3]);
        SetupHasIndexers();
        SetupEpisodeTrigger();

        await Service.SearchUpgradeEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Upgrade",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SameTypeInstances_IsolatedCooldowns()
    {
        var clientSeries = new Mock<IArrClient>();
        clientSeries.Setup(c => c.Instance).Returns("Series");
        clientSeries.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WantedEpisodeResource> { new() { Id = 1, Title = "Ep", SeasonNumber = 1, EpisodeNumber = 1 } });
        clientSeries.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientSeries.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var clientAnime = new Mock<IArrClient>();
        clientAnime.Setup(c => c.Instance).Returns("Anime");
        clientAnime.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WantedEpisodeResource> { new() { Id = 1, Title = "Ep", SeasonNumber = 1, EpisodeNumber = 1 } });
        clientAnime.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientAnime.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        SetupOutputCallback();
        SetupCleanExpiredAny();
        SetupCooldownIdsAny(ids: []);

        await Service.SearchMissingEpisodesAsync(clientSeries.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);
        await Service.SearchMissingEpisodesAsync(clientAnime.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        CooldownMock.Verify(c => c.CleanExpiredAsync("Series", "Missing",
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.CleanExpiredAsync("Anime", "Missing",
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.GetCooldownIdsAsync("Series", "Missing",
            It.IsAny<CancellationToken>()), Times.Once);
        CooldownMock.Verify(c => c.GetCooldownIdsAsync("Anime", "Missing",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_DoesNotFilterByMonitored()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Monitored = true, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 2, Monitored = false, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 3, Monitored = true, Title = "Ep3", SeasonNumber = 1, EpisodeNumber = 3,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var searchedIds = new List<int>();
        ClientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupHasIndexers();

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        Assert.Contains(2, searchedIds);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_DoesNotFilterByMonitored()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Monitored = true, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 2, Monitored = false, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        var searchedIds = new List<int>();
        ClientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        ClientMock.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: []);
        SetupHasIndexers();

        await Service.SearchUpgradeEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        Assert.Contains(2, searchedIds);
    }

    [Fact]
    public async Task SearchMissingEpisodes_NoIndexersAvailable_SkipsSearchAndCooldown()
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
        SetupHasIndexers(false);

        await Service.SearchMissingEpisodesAsync(ClientMock.Object, 5, DefaultCooldown, "episode", false, null, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
