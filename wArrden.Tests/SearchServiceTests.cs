using wArrden.Clients;
using wArrden.Clients.Models;
using wArrden.Services;

namespace wArrden.Tests;

public class SearchServiceTests
{
    private readonly Mock<ICooldownService> _cooldownMock;
    private readonly Mock<OutputService> _outputMock;
    private readonly Mock<IArrClient> _clientMock;
    private readonly SearchService _service;
    private static readonly TimeSpan DefaultCooldown = TimeSpan.FromDays(30);

    public SearchServiceTests()
    {
        _cooldownMock = new Mock<ICooldownService>();
        _outputMock = new Mock<OutputService>();
        _clientMock = new Mock<IArrClient>();
        _clientMock.Setup(c => c.Instance).Returns("Sonarr");

        _service = new SearchService(_cooldownMock.Object, _outputMock.Object);
    }

    [Fact]
    public async Task SearchMissingEpisodes_NoWantedItems_OutputsStatsWithZero()
    {
        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WantedEpisodeResource>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, "episode", false, CancellationToken.None);

        _cooldownMock.Verify(
            c => c.CleanExpiredAsync("Sonarr", "Missing", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_DryRun_DoesNotTriggerSearch()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 }
        };

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, "episode", true, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_DryRun_DoesNotTriggerSearch()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 }
        };

        _clientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchUpgradeEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, "episode", true, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchMissingMovies_DelegatesToRunMovieSearch()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" },
            new() { Id = 2, Title = "Movie 2" }
        };

        _clientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingMoviesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _cooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchUpgradeMovies_DelegatesToRunMovieSearch()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" }
        };

        _clientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchUpgradeMoviesAsync(_clientMock.Object, 3, DefaultCooldown, false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _cooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Upgrade",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
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

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _cooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingMovies_FullHappyPath_TriggersAndMarksCooldown()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" },
            new() { Id = 2, Title = "Movie B" },
            new() { Id = 3, Title = "Movie C" }
        };

        _clientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        _clientMock.Setup(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingMoviesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        _cooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_AllOnCooldown_NoSearchTriggered()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 },
            new() { Id = 2, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 2 }
        };

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int> { 1, 2 });

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int> { 3 });

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SearchMissingEpisodes_SameTypeInstancesIsolated()
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

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(clientSeries.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);
        await _service.SearchMissingEpisodesAsync(clientAnime.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);

        _cooldownMock.Verify(c => c.CleanExpiredAsync("Series", "Missing",
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.CleanExpiredAsync("Anime", "Missing",
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.GetCooldownIdsAsync("Series", "Missing",
            It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.GetCooldownIdsAsync("Anime", "Missing",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_FullHappyPath_TriggersAndMarksCooldown()
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

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "season", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(200, 2, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _cooldownMock.Verify(c => c.CleanExpiredAsync("Sonarr", "Missing_Season", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.GetCooldownIdsAsync("Sonarr", "Missing_Season", It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing_Season", It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchUpgradeEpisodes_SeasonSearch_FullHappyPath_TriggersAndMarksCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        _clientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchUpgradeEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "season", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Upgrade_Season", It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_DryRun_DoesNotTriggerSearch()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "season", true, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _cooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_NoWantedItems_OutputsStatsWithZero()
    {
        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WantedEpisodeResource>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, "season", false, CancellationToken.None);

        _cooldownMock.Verify(
            c => c.CleanExpiredAsync("Sonarr", "Missing_Season", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_AllOnCooldown_NoSearchTriggered()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1 },
            new() { Id = 2, SeriesId = 200, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 1 }
        };

        var seasonKey1 = 100 * 1000 + 1;
        var seasonKey2 = 200 * 1000 + 1;

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync("Sonarr", "Missing_Season", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { seasonKey1, seasonKey2 });

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "season", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_SomeOnCooldown_CorrectStats()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } },
            new() { Id = 2, SeriesId = 200, Title = "Ep2", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Another", Year = 2021 } }
        };

        var seasonKey1 = 100 * 1000 + 1;

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync("Sonarr", "Missing_Season", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { seasonKey1 });

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "season", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(200, 1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(100, 1, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_EpisodeSearchIndependentCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "season", false, CancellationToken.None);

        _cooldownMock.Verify(c => c.CleanExpiredAsync("Sonarr", "Missing_Season", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.GetCooldownIdsAsync("Sonarr", "Missing_Season", It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.CleanExpiredAsync("Sonarr", "Missing", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_MaxResultsLimitsSeasons()
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

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, "season", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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
        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);

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
        _clientMock.Setup(c => c.GetWantedCutoffEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _clientMock.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchUpgradeEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);

        Assert.Contains(2, searchedIds);
    }

    [Fact]
    public async Task SearchMissingMovies_DoesNotFilterByMonitored()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Monitored = true, Title = "Movie 1" },
            new() { Id = 2, Monitored = false, Title = "Movie 2" },
            new() { Id = 3, Monitored = true, Title = "Movie 3" }
        };

        var searchedIds = new List<int>();
        _clientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        _clientMock.Setup(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingMoviesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

        Assert.Contains(2, searchedIds);
    }

    [Fact]
    public async Task SearchUpgradeMovies_DoesNotFilterByMonitored()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Monitored = true, Title = "Movie 1" },
            new() { Id = 2, Monitored = false, Title = "Movie 2" }
        };

        var searchedIds = new List<int>();
        _clientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        _clientMock.Setup(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);
        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchUpgradeMoviesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

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

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "episode", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingEpisodes_SeasonSearch_NoIndexersAvailable_SkipsSearchAndCooldown()
    {
        var episodes = new List<WantedEpisodeResource>
        {
            new() { Id = 1, SeriesId = 100, Title = "Ep1", SeasonNumber = 1, EpisodeNumber = 1,
                Series = new WantedEpisodeSeriesResource { Title = "Show", Year = 2020 } }
        };

        _clientMock.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, "season", false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing_Season",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingMovies_NoIndexersAvailable_SkipsSearchAndCooldown()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" }
        };

        _clientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        _cooldownMock.Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cooldownMock.Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _clientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        _outputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new TestSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);

        await _service.SearchMissingMoviesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

        _clientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private class TestSearchOutputWriter : OutputService.SearchOutputWriter
    {
        public TestSearchOutputWriter() : base("test", "Missing Search", 10, TextWriter.Null) { }

        public override void WriteHeader() { }
        public override void SetPhase(string phase) { }
        public override void WriteStats(int totalCount, int onCooldown, int eligible, int searched, bool isLast, string? resultOverride = null) { }
        public override void StartResults() { }
        public override void WriteItem(string title) { }
        public override void WriteTrailer() { }
    }
}
