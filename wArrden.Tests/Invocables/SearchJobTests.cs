using wArrden.Clients;
using wArrden.Invocables;
using wArrden.Services;

namespace wArrden.Tests;

public class SearchJobTests
{
    private readonly Mock<SearchService> _searchMock;
    private readonly Mock<IArrClient> _clientMock;
    private readonly OutputService _output;

    public SearchJobTests()
    {
        var nullOutput = new OutputService { Out = TextWriter.Null, Error = TextWriter.Null };
        var taggingMock = new Mock<TaggingService>(new OutputService { Out = TextWriter.Null, Error = TextWriter.Null }, null!);
        _searchMock = new Mock<SearchService>(Mock.Of<ICooldownService>(), nullOutput, taggingMock.Object) { CallBase = true };
        _clientMock = new Mock<IArrClient>();
        _clientMock.Setup(c => c.Instance).Returns("Sonarr");
        _output = new OutputService { Out = TextWriter.Null, Error = TextWriter.Null };
    }

    [Fact]
    public void Invoke_MissingSonarr_CallsSearchMissingEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "sonarr", 10, "30d", "episode", false, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "episode", false, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeSonarr_CallsSearchUpgradeEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "upgrade", "sonarr", 5, "7d", "episode", true, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "episode", true, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingRadarr_CallsSearchMissingMovies()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "radarr", 20, "12h", "episode", false, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingMoviesAsync(
            _clientMock.Object, 20, TimeSpan.FromHours(12), false, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeRadarr_CallsSearchUpgradeMovies()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "upgrade", "radarr", 3, "90m", "episode", true, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeMoviesAsync(
            _clientMock.Object, 3, TimeSpan.FromMinutes(90), true, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Invoke_UnknownCombo_ThrowsInvalidOperationException()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "unknown", "sonarr", 10, "30d", "episode", false, null, null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Invoke());
    }

    [Fact]
    public void Constructor_ValidCooldown_ParsesCorrectly()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "sonarr", 10, "7d", "episode", false, null, null));

        var task = job.Invoke();
        Assert.NotNull(task);
    }

    [Fact]
    public void Constructor_InvalidCooldown_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
                "missing", "sonarr", 10, "invalid", "episode", false, null, null)));
    }

    [Fact]
    public void Invoke_MissingSonarr_SeasonSearchType_CallsWithSeason()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "sonarr", 10, "30d", "season", false, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "season", false, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeSonarr_SeasonSearchType_CallsWithSeason()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "upgrade", "sonarr", 5, "7d", "season", true, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "season", true, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_IndexerNames_PassedToService()
    {
        var indexerNames = new List<string> { "NZBGeek", "Rarbg" };
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "sonarr", 10, "30d", "episode", false, indexerNames, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "episode", false, indexerNames, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingWhisparr_CallsSearchMissingEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "whisparr", 10, "30d", "episode", false, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingEpisodesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "episode", false, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeWhisparr_CallsSearchUpgradeEpisodes()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "upgrade", "whisparr", 5, "7d", "season", true, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeEpisodesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), "season", true, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingWhisparrEros_CallsSearchMissingMovies()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "whisparr-eros", 10, "30d", "", false, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingMoviesAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), false, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeWhisparrEros_CallsSearchUpgradeMovies()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "upgrade", "whisparr-eros", 5, "7d", "", true, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeMoviesAsync(
            _clientMock.Object, 5, TimeSpan.FromDays(7), true, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingLidarr_Album_CallsSearchMissingAlbums()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "lidarr", 10, "30d", "album", false, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingAlbumsAsync(
            _clientMock.Object, 10, TimeSpan.FromDays(30), "album", false, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_MissingLidarr_Artist_CallsSearchMissingAlbums()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "missing", "lidarr", 15, "7d", "artist", true, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchMissingAlbumsAsync(
            _clientMock.Object, 15, TimeSpan.FromDays(7), "artist", true, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeLidarr_Album_CallsSearchUpgradeAlbums()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "upgrade", "lidarr", 5, "90m", "album", false, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeAlbumsAsync(
            _clientMock.Object, 5, TimeSpan.FromMinutes(90), "album", false, null, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Invoke_UpgradeLidarr_Artist_CallsSearchUpgradeAlbums()
    {
        var job = new SearchJob(_searchMock.Object, _output, new SearchJobParams(_clientMock.Object,
            "upgrade", "lidarr", 3, "12h", "artist", true, null, null));

        job.Invoke();

        _searchMock.Verify(s => s.SearchUpgradeAlbumsAsync(
            _clientMock.Object, 3, TimeSpan.FromHours(12), "artist", true, null, null, CancellationToken.None), Times.Once);
    }
}
