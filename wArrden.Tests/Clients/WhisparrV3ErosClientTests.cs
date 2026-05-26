using System.Net;
using System.Text.Json;
using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class WhisparrV3ErosClientTests
{
    [Fact]
    public async Task GetQueueAsync_UrlContainsIncludeMovie()
    {
        var handler = new FakeHttpMessageHandler(EmptyQueueJson);
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        await client.GetQueueAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("api/v3/queue", handler.LastRequestUri);
        Assert.Contains("includeUnknownMovieItems=true", handler.LastRequestUri);
        Assert.Contains("includeMovie=true", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedMissingMovies_UrlContainsMonitoredTrueAndMovieSort()
    {
        var handler = new FakeHttpMessageHandler(EmptyMoviePageJson);
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        await client.GetWantedMissingMoviesAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("wanted/missing", handler.LastRequestUri);
        Assert.Contains("monitored=true", handler.LastRequestUri);
        Assert.Contains("sortKey=movies.lastSearchTime", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedCutoffMovies_UrlContainsMonitoredTrueAndMovieSort()
    {
        var handler = new FakeHttpMessageHandler(EmptyMoviePageJson);
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        await client.GetWantedCutoffMoviesAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("wanted/cutoff", handler.LastRequestUri);
        Assert.Contains("monitored=true", handler.LastRequestUri);
        Assert.Contains("sortKey=movies.lastSearchTime", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedMissingMovies_ParsesMovieResources()
    {
        var json = BuildMoviePage(new[]
        {
            (1, "Eros Movie", true, 2026)
        });
        var handler = new FakeHttpMessageHandler(json);
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        var result = await client.GetWantedMissingMoviesAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Eros Movie", result[0].Title);
        Assert.True(result[0].Monitored);
        Assert.Equal(2026, result[0].Year);
    }

    [Fact]
    public async Task TriggerMoviesSearchAsync_PostsMoviesSearchCommand()
    {
        var handler = new FakeHttpMessageHandler("{}");
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        await client.TriggerMoviesSearchAsync(new[] { 1, 2 }, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("/api/v3/command", handler.LastRequestUri);
        Assert.NotNull(handler.LastRequestBody);

        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        Assert.Equal("MoviesSearch", doc.RootElement.GetProperty("name").GetString());
        var movieIds = doc.RootElement.GetProperty("movieIds").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        Assert.Equal(new[] { 1, 2 }, movieIds);
    }

    [Fact]
    public async Task GetWantedMissingEpisodes_ThrowsNotSupportedException()
    {
        using IArrClient client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", new HttpClientHandler());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.GetWantedMissingEpisodesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task TriggerEpisodeSearch_ThrowsNotSupportedException()
    {
        using IArrClient client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", new HttpClientHandler());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.TriggerEpisodeSearchAsync(new[] { 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task TriggerSeasonSearch_ThrowsNotSupportedException()
    {
        using IArrClient client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", new HttpClientHandler());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.TriggerSeasonSearchAsync(1, 1, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsTrue_WhenApiReturns200()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("/api", handler.LastRequestUri);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsFalse_WhenApiReturns401()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized);
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsFalse_WhenConnectionFails()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new WhisparrV3ErosClient("http://localhost", "key", "Whisparr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.False(result);
    }

    private static string EmptyQueueJson => JsonSerializer.Serialize(new
    {
        page = 1,
        pageSize = 100,
        totalRecords = 0,
        records = Array.Empty<object>()
    });

    private static string EmptyMoviePageJson => BuildMoviePage(Array.Empty<(int, string, bool, int)>());

    private static string BuildMoviePage((int Id, string Title, bool Monitored, int Year)[] movies)
    {
        var records = movies.Select(m => new
        {
            id = m.Id,
            title = m.Title,
            monitored = m.Monitored,
            hasFile = false,
            lastSearchTime = (DateTime?)null,
            year = m.Year
        }).ToArray();

        var page = new
        {
            page = 1,
            pageSize = 100,
            totalRecords = records.Length,
            records
        };

        return JsonSerializer.Serialize(page);
    }
}
