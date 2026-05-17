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

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, false, CancellationToken.None);

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

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, true, CancellationToken.None);

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

        await _service.SearchUpgradeEpisodesAsync(_clientMock.Object, 2, DefaultCooldown, true, CancellationToken.None);

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

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

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

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

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

        await _service.SearchMissingEpisodesAsync(_clientMock.Object, 5, DefaultCooldown, false, CancellationToken.None);

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

        var clientAnime = new Mock<IArrClient>();
        clientAnime.Setup(c => c.Instance).Returns("Anime");
        clientAnime.Setup(c => c.GetWantedMissingEpisodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WantedEpisodeResource> { new() { Id = 1, Title = "Ep", SeasonNumber = 1, EpisodeNumber = 1 } });
        clientAnime.Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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

        await _service.SearchMissingEpisodesAsync(clientSeries.Object, 5, DefaultCooldown, false, CancellationToken.None);
        await _service.SearchMissingEpisodesAsync(clientAnime.Object, 5, DefaultCooldown, false, CancellationToken.None);

        _cooldownMock.Verify(c => c.CleanExpiredAsync("Series", "Missing",
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.CleanExpiredAsync("Anime", "Missing",
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.GetCooldownIdsAsync("Series", "Missing",
            It.IsAny<CancellationToken>()), Times.Once);
        _cooldownMock.Verify(c => c.GetCooldownIdsAsync("Anime", "Missing",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private class TestSearchOutputWriter : OutputService.SearchOutputWriter
    {
        public TestSearchOutputWriter() : base("test", "Missing Search", 10, TextWriter.Null) { }

        public override void WriteHeader() { }
        public override void SetPhase(string phase) { }
        public override void WriteStats(int totalCount, int onCooldown, int eligible, int searched, bool isLast) { }
        public override void StartResults() { }
        public override void WriteItem(string title) { }
        public override void WriteTrailer() { }
    }
}
