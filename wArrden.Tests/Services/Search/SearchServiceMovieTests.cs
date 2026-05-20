using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class SearchServiceMovieTests : SearchServiceTestBase
{
    [Fact]
    public async Task SearchMissingMovies_DryRun_DoesNotTriggerSearch()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" }
        };

        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, true, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchUpgradeMovies_DryRun_DoesNotTriggerSearch()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" }
        };

        ClientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: []);

        await Service.SearchUpgradeMoviesAsync(ClientMock.Object, 5, DefaultCooldown, true, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingMovies_FullHappyPath_TriggersAndMarksCooldown()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" },
            new() { Id = 2, Title = "Movie B" }
        };

        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupHasIndexers();
        SetupMovieTrigger();

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchUpgradeMovies_FullHappyPath_TriggersAndMarksCooldown()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie 1" }
        };

        ClientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: []);
        SetupHasIndexers();
        SetupMovieTrigger();

        await Service.SearchUpgradeMoviesAsync(ClientMock.Object, 3, DefaultCooldown, false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Upgrade",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchMissingMovies_AllOnCooldown_NoSearchTriggered()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" },
            new() { Id = 2, Title = "Movie B" }
        };

        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: [1, 2]);

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchUpgradeMovies_AllOnCooldown_NoSearchTriggered()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" }
        };

        ClientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: [1]);

        await Service.SearchUpgradeMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMissingMovies_SomeOnCooldown_CorrectStats()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" },
            new() { Id = 2, Title = "Movie B" },
            new() { Id = 3, Title = "Movie C" }
        };

        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: [3]);
        SetupHasIndexers();
        SetupMovieTrigger();

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Missing",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchUpgradeMovies_SomeOnCooldown_CorrectStats()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" },
            new() { Id = 2, Title = "Movie B" },
            new() { Id = 3, Title = "Movie C" }
        };

        ClientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: [3]);
        SetupHasIndexers();
        SetupMovieTrigger();

        await Service.SearchUpgradeMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        CooldownMock.Verify(c => c.MarkSearchedAsync("Sonarr", "Upgrade",
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Once);
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
        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        ClientMock.Setup(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupHasIndexers();

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

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
        ClientMock.Setup(c => c.GetWantedCutoffMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        ClientMock.Setup(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Callback<int[], CancellationToken>((ids, _) => searchedIds.AddRange(ids))
            .Returns(Task.CompletedTask);

        SetupOutputCallback();
        SetupCleanExpired(category: "Upgrade");
        SetupCooldownIds(category: "Upgrade", ids: []);
        SetupHasIndexers();

        await Service.SearchUpgradeMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

        Assert.Contains(2, searchedIds);
    }

    [Fact]
    public async Task SearchMissingMovies_NoIndexersAvailable_SkipsSearchAndCooldown()
    {
        var movies = new List<WantedMovieResource>
        {
            new() { Id = 1, Title = "Movie A" }
        };

        ClientMock.Setup(c => c.GetWantedMissingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);

        SetupOutputCallback();
        SetupCleanExpired();
        SetupCooldownIds(ids: []);
        SetupHasIndexers(false);

        await Service.SearchMissingMoviesAsync(ClientMock.Object, 5, DefaultCooldown, false, null, CancellationToken.None);

        ClientMock.Verify(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        CooldownMock.Verify(c => c.MarkSearchedAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
