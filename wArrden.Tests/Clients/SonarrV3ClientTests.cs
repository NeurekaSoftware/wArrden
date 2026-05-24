using System.Net;
using System.Text.Json;
using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class SonarrV3ClientTests
{
    [Fact]
    public async Task GetWantedMissingEpisodes_UrlContainsMonitoredTrue()
    {
        var handler = new FakeHttpMessageHandler(EmptyEpisodePageJson);
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        await client.GetWantedMissingEpisodesAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("monitored=true", handler.LastRequestUri);
        Assert.Contains("wanted/missing", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedCutoffEpisodes_UrlContainsMonitoredTrue()
    {
        var handler = new FakeHttpMessageHandler(EmptyEpisodePageJson);
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        await client.GetWantedCutoffEpisodesAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("monitored=true", handler.LastRequestUri);
        Assert.Contains("wanted/cutoff", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedMissingEpisodes_FiltersOutUnmonitoredItems()
    {
        var json = BuildEpisodePage(new[]
        {
            (1, "Monitored Ep", true),
            (2, "Unmonitored Ep", false),
            (3, "Another Monitored", true)
        });
        var handler = new FakeHttpMessageHandler(json);
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        var result = await client.GetWantedMissingEpisodesAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.True(e.Monitored));
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 3);
        Assert.DoesNotContain(result, e => e.Id == 2);
    }

    [Fact]
    public async Task GetWantedCutoffEpisodes_FiltersOutUnmonitoredItems()
    {
        var json = BuildEpisodePage(new[]
        {
            (1, "Monitored Ep", true),
            (2, "Unmonitored Ep", false)
        });
        var handler = new FakeHttpMessageHandler(json);
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        var result = await client.GetWantedCutoffEpisodesAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.True(result[0].Monitored);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetWantedMissingEpisodes_AllUnmonitored_ReturnsEmptyList()
    {
        var json = BuildEpisodePage(new[]
        {
            (1, "Unmonitored 1", false),
            (2, "Unmonitored 2", false)
        });
        var handler = new FakeHttpMessageHandler(json);
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        var result = await client.GetWantedMissingEpisodesAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsTrue_WhenApiReturns200()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("/api", handler.LastRequestUri);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsFalse_WhenApiReturns401()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized);
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsFalse_WhenConnectionFails()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new SonarrV3Client("http://localhost", "key", "Sonarr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.False(result);
    }

    private static string EmptyEpisodePageJson => BuildEpisodePage(Array.Empty<(int, string, bool)>());

    private static string BuildEpisodePage((int Id, string Title, bool Monitored)[] episodes)
    {
        var records = episodes.Select(e => new
        {
            id = e.Id,
            seriesId = e.Id * 100,
            series = new { title = e.Title, year = 2020 },
            seasonNumber = 1,
            episodeNumber = e.Id,
            title = e.Title,
            monitored = e.Monitored,
            hasFile = false,
            lastSearchTime = (DateTime?)null,
            airDateUtc = (DateTime?)null
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
