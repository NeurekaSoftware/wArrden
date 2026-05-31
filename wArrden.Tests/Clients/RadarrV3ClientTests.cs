using System.Net;
using System.Text.Json;
using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class RadarrV3ClientTests
{
    [Fact]
    public async Task GetWantedMissingMovies_UrlContainsMonitoredTrue()
    {
        var handler = new FakeHttpMessageHandler(EmptyMoviePageJson);
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        await client.GetWantedMissingMoviesAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("monitored=true", handler.LastRequestUri);
        Assert.Contains("wanted/missing", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedCutoffMovies_UrlContainsMonitoredTrue()
    {
        var handler = new FakeHttpMessageHandler(EmptyMoviePageJson);
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        await client.GetWantedCutoffMoviesAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("monitored=true", handler.LastRequestUri);
        Assert.Contains("wanted/cutoff", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedMissingMovies_UsesServerSideMonitoringFilter()
    {
        var json = BuildMoviePage(new[]
        {
            (1, "Monitored Movie", true, 2020),
            (2, "Another Monitored", true, 2022)
        });
        var handler = new FakeHttpMessageHandler(json);
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        var result = await client.GetWantedMissingMoviesAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.True(m.Monitored));
    }

    [Fact]
    public async Task GetWantedCutoffMovies_UsesServerSideMonitoringFilter()
    {
        var json = BuildMoviePage(new[]
        {
            (1, "Monitored Movie", true, 2020)
        });
        var handler = new FakeHttpMessageHandler(json);
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        var result = await client.GetWantedCutoffMoviesAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.True(result[0].Monitored);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetWantedMissingMovies_EmptyPage_ReturnsEmptyResults()
    {
        var json = EmptyMoviePageJson;
        var handler = new FakeHttpMessageHandler(json);
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        var result = await client.GetWantedMissingMoviesAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsTrue_WhenApiReturns200()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("/api", handler.LastRequestUri);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsFalse_WhenApiReturns401()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized);
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_Throws_WhenConnectionFails()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new RadarrV3Client("http://localhost", "key", "Radarr", handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.ValidateApiKeyAsync(CancellationToken.None));
    }

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
