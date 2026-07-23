using System.Net;
using System.Text.Json;
using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Tests;

public class LidarrV1ClientTests
{
    [Fact]
    public async Task GetQueueAsync_UrlContainsIncludeArtistAndAlbum()
    {
        var handler = new FakeHttpMessageHandler(EmptyQueueJson);
        var client = new LidarrV1Client("http://localhost", "key", "Lidarr", handler);

        await client.GetQueueAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("api/v1/queue", handler.LastRequestUri);
        Assert.Contains("includeUnknownArtistItems=true", handler.LastRequestUri);
        Assert.Contains("includeArtist=true", handler.LastRequestUri);
        Assert.Contains("includeAlbum=true", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetQueueAsync_ParsesQueueRecords()
    {
        var json = BuildQueueJson(new[]
        {
            (1, "The Beatles", "Abbey Road")
        });
        var handler = new FakeHttpMessageHandler(json);
        var client = new LidarrV1Client("http://localhost", "key", "Lidarr", handler);

        var result = await client.GetQueueAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.NotNull(result[0].Artist);
        Assert.Equal("The Beatles", result[0].Artist!.ArtistName);
        Assert.NotNull(result[0].Album);
        Assert.Equal("Abbey Road", result[0].Album!.Title);
    }

    [Fact]
    public async Task DeleteQueueItemAsync_UrlContainsBlocklistTrue()
    {
        var handler = new FakeHttpMessageHandler("{}");
        var client = new LidarrV1Client("http://localhost", "key", "Lidarr", handler);

        await client.DeleteQueueItemAsync(42, CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("/api/v1/queue/42", handler.LastRequestUri);
        Assert.Contains("blocklist=true", handler.LastRequestUri);
        Assert.Contains("removeFromClient=true", handler.LastRequestUri);
    }

    [Fact]
    public async Task DeleteQueueItemWithoutBlocklistAsync_UrlContainsBlocklistFalse()
    {
        var handler = new FakeHttpMessageHandler("{}");
        var client = new LidarrV1Client("http://localhost", "key", "Lidarr", handler);

        await client.DeleteQueueItemWithoutBlocklistAsync(42, CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("/api/v1/queue/42", handler.LastRequestUri);
        Assert.Contains("blocklist=false", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetWantedMissingEpisodes_ThrowsNotSupportedException()
    {
        using IArrClient client = new LidarrV1Client("http://localhost", "key", "Lidarr", new HttpClientHandler());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.GetWantedMissingEpisodesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task TriggerEpisodeSearch_ThrowsNotSupportedException()
    {
        using IArrClient client = new LidarrV1Client("http://localhost", "key", "Lidarr", new HttpClientHandler());
        await Assert.ThrowsAsync<NotSupportedException>(() => client.TriggerEpisodeSearchAsync(new[] { 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsOk_WhenApiReturns200()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var client = new LidarrV1Client("http://localhost", "key", "Lidarr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, result);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("api/v1/system/status", handler.LastRequestUri);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ReturnsUnauthorized_WhenApiReturns401()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized);
        var client = new LidarrV1Client("http://localhost", "key", "Lidarr", handler);

        var result = await client.ValidateApiKeyAsync(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_Throws_WhenConnectionFails()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new LidarrV1Client("http://localhost", "key", "Lidarr", handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.ValidateApiKeyAsync(CancellationToken.None));
    }

    private static string EmptyQueueJson => BuildQueueJson(Array.Empty<(int, string, string)>());

    private static string BuildQueueJson((int Id, string ArtistName, string AlbumTitle)[] items)
    {
        var records = items.Select(i => new
        {
            id = i.Id,
            title = $"{i.ArtistName} - {i.AlbumTitle}",
            artist = new { id = i.Id * 100, artistName = i.ArtistName },
            album = new { id = i.Id * 10, title = i.AlbumTitle },
            quality = new { quality = new { id = 1, name = "FLAC" }, revision = new { version = 1, real = 0 } },
            size = (long)30000000,
            sizeleft = (long)0,
            timeleft = (string?)null,
            estimatedCompletionTime = (DateTime?)null,
            status = "completed",
            trackedDownloadStatus = (string?)null,
            trackedDownloadState = (string?)null,
            statusMessages = Array.Empty<object>(),
            errorMessage = (string?)null,
            downloadId = $"download_{i.Id}",
            protocol = "usenet",
            indexer = "NZBGeek",
            outputPath = $"/music/{i.ArtistName}/{i.AlbumTitle}"
        }).ToArray();

        var page = new { page = 1, pageSize = 100, totalRecords = records.Length, records };
        return JsonSerializer.Serialize(page);
    }
}
