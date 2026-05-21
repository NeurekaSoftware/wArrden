using wArrden.Clients;
using wArrden.Invocables;
using wArrden.Services;

namespace wArrden.Tests;

public class SearchJobTests
{
    private readonly Mock<SearchService> _searchMock;
    private readonly Mock<IArrClient> _clientMock;

    public SearchJobTests()
    {
        _searchMock = new Mock<SearchService>(Mock.Of<ICooldownService>(), new OutputService());
        _clientMock = new Mock<IArrClient>();
    }

    [Fact]
    public void Invoke_MissingSonarr_CallsSearchMissingEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "sonarr", 10, "30d", "episode", false, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "episode", false, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeSonarr_CallsSearchUpgradeEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "sonarr", 5, "7d", "episode", true, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "episode", true, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingRadarr_CallsSearchMissingMovies()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "radarr", 20, "12h", "episode", false, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingMoviesAsync(
            _clientMock.Object, 20, TimeSpan.FromHours(12), false, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeRadarr_CallsSearchUpgradeMovies()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "radarr", 3, "90m", "episode", true, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeMoviesAsync(
            _clientMock.Object, 3, TimeSpan.FromMinutes(90), true, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UnknownCombo_ReturnsCompletedTask()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "unknown", "sonarr", 10, "30d", "episode", false, null);

        var task = job.Invoke();

        Assert.Equal(Task.CompletedTask, task);
        _searchMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Constructor_ValidCooldown_ParsesCorrectly()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "sonarr", 10, "7d", "episode", false, null);

        var task = job.Invoke();
        Assert.NotNull(task);
    }

    [Fact]
    public void Constructor_InvalidCooldown_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SearchJob(_searchMock.Object, _clientMock.Object,
                "missing", "sonarr", 10, "invalid", "episode", false, null));
    }

    [Fact]
    public void Invoke_MissingSonarr_SeasonSearchType_CallsWithSeason()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "sonarr", 10, "30d", "season", false, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "season", false, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeSonarr_SeasonSearchType_CallsWithSeason()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "sonarr", 5, "7d", "season", true, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "season", true, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_IndexerNames_PassedToService()
    {
        var indexerNames = new List<string> { "NZBGeek", "Rarbg" };
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "sonarr", 10, "30d", "episode", false, indexerNames);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "episode", false, indexerNames, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingWhisparr_CallsSearchMissingEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "whisparr", 10, "30d", "episode", false, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "episode", false, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeWhisparr_CallsSearchUpgradeEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "whisparr", 5, "7d", "season", true, null);

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "season", true, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingLidarr_ReturnsCompletedTask()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "lidarr", 10, "30d", "album", false, null);

        var task = job.Invoke();

        Assert.Equal(Task.CompletedTask, task);
        _searchMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Invoke_UpgradeLidarr_ReturnsCompletedTask()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "lidarr", 5, "7d", "artist", false, null);

        var task = job.Invoke();

        Assert.Equal(Task.CompletedTask, task);
        _searchMock.VerifyNoOtherCalls();
    }
}
