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
            "missing", "sonarr", 10, "30d", "episode", false);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "episode", false, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeSonarr_CallsSearchUpgradeEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "sonarr", 5, "7d", "episode", true);

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "episode", true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingRadarr_CallsSearchMissingMovies()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "radarr", 20, "12h", "episode", false);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingMoviesAsync(
            _clientMock.Object, 20, TimeSpan.FromHours(12), false, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeRadarr_CallsSearchUpgradeMovies()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "radarr", 3, "90m", "episode", true);

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeMoviesAsync(
            _clientMock.Object, 3, TimeSpan.FromMinutes(90), true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UnknownCombo_ReturnsCompletedTask()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "unknown", "sonarr", 10, "30d", "episode", false);

        var task = job.Invoke();

        Assert.Equal(Task.CompletedTask, task);
        _searchMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Constructor_ValidCooldown_ParsesCorrectly()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "sonarr", 10, "7d", "episode", false);

        var task = job.Invoke();
        Assert.NotNull(task);
    }

    [Fact]
    public void Constructor_InvalidCooldown_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SearchJob(_searchMock.Object, _clientMock.Object,
                "missing", "sonarr", 10, "invalid", "episode", false));
    }

    [Fact]
    public void Invoke_MissingSonarr_SeasonSearchType_CallsWithSeason()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "missing", "sonarr", 10, "30d", "season", false);

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "season", false, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeSonarr_SeasonSearchType_CallsWithSeason()
    {
        var job = new SearchJob(_searchMock.Object, _clientMock.Object,
            "upgrade", "sonarr", 5, "7d", "season", true);

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "season", true, CancellationToken.None), Times.Once);
    }
}
