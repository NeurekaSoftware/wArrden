using System.Text.Json;
using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class WhisparrV3ClientTests
{
    [Fact]
    public async Task GetQueueAsync_UrlContainsIncludeSeriesAndEpisode()
    {
        var handler = new FakeHttpMessageHandler(EmptyQueueJson);
        var client = new WhisparrV3Client("http://localhost", "key", "Whisparr", handler);

        await client.GetQueueAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("api/v3/queue", handler.LastRequestUri);
        Assert.Contains("includeUnknownSeriesItems=true", handler.LastRequestUri);
        Assert.Contains("includeSeries=true", handler.LastRequestUri);
        Assert.Contains("includeEpisode=true", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedMissingEpisodes_UrlContainsMonitoredTrue()
    {
        var handler = new FakeHttpMessageHandler(EmptyEpisodePageJson);
        var client = new WhisparrV3Client("http://localhost", "key", "Whisparr", handler);

        await client.GetWantedMissingEpisodesAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("monitored=true", handler.LastRequestUri);
        Assert.Contains("wanted/missing", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedCutoffEpisodes_UrlContainsMonitoredTrue()
    {
        var handler = new FakeHttpMessageHandler(EmptyEpisodePageJson);
        var client = new WhisparrV3Client("http://localhost", "key", "Whisparr", handler);

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
        var client = new WhisparrV3Client("http://localhost", "key", "Whisparr", handler);

        var result = await client.GetWantedMissingEpisodesAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.True(e.Monitored));
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 3);
        Assert.DoesNotContain(result, e => e.Id == 2);
    }

    [Fact]
    public async Task GetWantedMissingMovies_ThrowsNotSupportedException()
    {
        using IArrClient client = new WhisparrV3Client("http://localhost", "key", "Whisparr", new HttpClientHandler());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.GetWantedMissingMoviesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task TriggerMoviesSearch_ThrowsNotSupportedException()
    {
        using IArrClient client = new WhisparrV3Client("http://localhost", "key", "Whisparr", new HttpClientHandler());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.TriggerMoviesSearchAsync(new[] { 1 }, CancellationToken.None));
    }

    private static string EmptyQueueJson => JsonSerializer.Serialize(new
    {
        page = 1,
        pageSize = 100,
        totalRecords = 0,
        records = Array.Empty<object>()
    });

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

        var page = new { page = 1, pageSize = 100, totalRecords = records.Length, records };
        return JsonSerializer.Serialize(page);
    }
}
