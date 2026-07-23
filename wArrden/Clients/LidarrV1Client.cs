using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using wArrden.Clients.Models;

namespace wArrden.Clients;

public class LidarrV1Client : IArrClient
{
    private readonly HttpClient _http;
    private bool _disposed;

    public string Instance { get; }

    public LidarrV1Client(HttpClient http, string instanceName)
    {
        Instance = instanceName;
        _http = http;
    }

    // Test seam: wraps the supplied handler in an HttpClient carrying the same base address
    // and API-key header that the factory-configured client uses in production.
    internal LidarrV1Client(string url, string apiKey, string instanceName, HttpMessageHandler handler)
        : this(BuildTestHttpClient(url, apiKey, handler), instanceName)
    {
    }

    private static HttpClient BuildTestHttpClient(string url, string apiKey, HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(url.TrimEnd('/') + "/") };
        http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return http;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<IReadOnlyList<QueueResource>> GetQueueAsync(CancellationToken ct)
    {
        var byId = new Dictionary<int, QueueResource>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"api/v1/queue?includeUnknownArtistItems=true&includeArtist=true&includeAlbum=true&page={page}&pageSize={pageSize}";
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.WantedPagingResourceQueueResource, ct);
            var records = paging?.Records;
            if (records is not { Count: > 0 })
                break;

            // Default queue ordering is mutable, so records can repeat across pages;
            // dedupe by Id to avoid processing the same queue item twice.
            foreach (var r in records)
                byId[r.Id] = r;

            // Terminate on a short/empty page, never on a duplicate-inflated count.
            if (records.Count < pageSize || byId.Count >= paging!.TotalRecords)
                break;

            page++;
        }

        return byId.Values.ToList();
    }

    public async Task DeleteQueueItemAsync(int queueId, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"api/v1/queue/{queueId}?blocklist=true&removeFromClient=true", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQueueItemWithoutBlocklistAsync(int queueId, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"api/v1/queue/{queueId}?blocklist=false&removeFromClient=true", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<IndexerResource>> GetIndexersAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync($"api/v1/indexer", ct);
        response.EnsureSuccessStatusCode();
        return (IReadOnlyList<IndexerResource>?)await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.IndexerResourceArray, ct) ?? Array.Empty<IndexerResource>();
    }

    public async Task<bool> HasAnyEnabledIndexerAsync(CancellationToken ct)
    {
        var indexers = await GetIndexersAsync(ct);
        return indexers.Any(i => i.EnableAutomaticSearch);
    }

    public async Task<IReadOnlyList<WantedAlbumResource>> GetWantedMissingAlbumsAsync(CancellationToken ct)
    {
        return await FetchAllWantedAlbumPagesAsync("missing", ct);
    }

    public async Task<IReadOnlyList<WantedAlbumResource>> GetWantedCutoffAlbumsAsync(CancellationToken ct)
    {
        return await FetchAllWantedAlbumPagesAsync("cutoff", ct);
    }

    private async Task<IReadOnlyList<WantedAlbumResource>> FetchAllWantedAlbumPagesAsync(string type, CancellationToken ct)
    {
        var byId = new Dictionary<int, WantedAlbumResource>(capacity: 100);
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"api/v1/wanted/{type}?includeArtist=true&monitored=true&page={page}&pageSize={pageSize}&sortKey=albums.lastSearchTime&sortDirection=ascending";
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.WantedPagingResourceWantedAlbumResource, ct);
            var records = paging?.Records;
            if (records is not { Count: > 0 })
                break;

            // Sort key (lastSearchTime) is non-unique and mutable, so records can repeat
            // across pages; dedupe by Id to avoid duplicate cooldown inserts.
            foreach (var r in records)
                byId[r.Id] = r;

            // Terminate on a short/empty page, never on a duplicate-inflated count.
            if (records.Count < pageSize || byId.Count >= paging!.TotalRecords)
                break;

            page++;
        }

        return byId.Values.ToList();
    }

    public async Task TriggerAlbumSearchAsync(int[] albumIds, CancellationToken ct)
    {
        var body = new { name = "AlbumSearch", albumIds };
        await PostCommandAsync(body, ct);
    }

    public async Task TriggerArtistSearchAsync(int artistId, CancellationToken ct)
    {
        var body = new { name = "ArtistSearch", artistId };
        await PostCommandAsync(body, ct);
    }

    private async Task PostCommandAsync(object command, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync($"api/v1/command", command, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
    }

    Task<IReadOnlyList<WantedEpisodeResource>> IArrClient.GetWantedMissingEpisodesAsync(CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support episode wanted endpoints.");

    Task<IReadOnlyList<WantedEpisodeResource>> IArrClient.GetWantedCutoffEpisodesAsync(CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support episode wanted endpoints.");

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedMissingMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support movie wanted endpoints.");

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedCutoffMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support movie wanted endpoints.");

    Task IArrClient.TriggerEpisodeSearchAsync(int[] episodeIds, CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support episode search.");

    Task IArrClient.TriggerSeasonSearchAsync(int seriesId, int seasonNumber, CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support season search.");

    Task IArrClient.TriggerMoviesSearchAsync(int[] movieIds, CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support movie search.");

    public async Task<IReadOnlyList<TagResource>> GetTagsAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync($"api/v1/tag", ct);
        response.EnsureSuccessStatusCode();
        return (IReadOnlyList<TagResource>?)await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.ListTagResource, ct) ?? Array.Empty<TagResource>();
    }

    public async Task<TagResource> CreateTagAsync(string label, CancellationToken ct)
    {
        var body = JsonContent.Create(new { label });
        using var response = await _http.PostAsync($"api/v1/tag", body, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.TagResource, ct))!;
    }

    public Task<bool> EnsureTagOnSeriesAsync(int seriesId, int tagId, CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support series tags.");

    public Task<bool> EnsureTagOnMovieAsync(int movieId, int tagId, CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support movie tags.");

    public async Task<bool> EnsureTagOnArtistAsync(int artistId, int tagId, CancellationToken ct)
    {
        return await EnsureTagOnResourceAsync("artist", artistId, tagId, ct);
    }

    public Task<HashSet<int>> ResolveSeriesIdsAsync(int[] episodeIds, CancellationToken ct)
        => throw new NotSupportedException("Lidarr does not support series resolution.");

    public async Task<HashSet<int>> ResolveArtistIdsAsync(int[] albumIds, CancellationToken ct)
    {
        var artistIds = new HashSet<int>();
        const int batchSize = 50;
        for (var i = 0; i < albumIds.Length; i += batchSize)
        {
            var batch = albumIds.Skip(i).Take(batchSize);
            var idsParam = string.Join("&albumIds=", batch);
            using var response = await _http.GetAsync(
                $"api/v1/album?albumIds={idsParam}", ct);
            response.EnsureSuccessStatusCode();

            var root = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
            if (root is null) continue;

            foreach (var album in root.AsArray())
            {
                var aid = album?["artistId"]?.GetValue<int>();
                if (aid.HasValue && aid.Value != 0)
                    artistIds.Add(aid.Value);
            }
        }
        return artistIds;
    }

    private async Task<bool> EnsureTagOnResourceAsync(string resourceType, int resourceId, int tagId, CancellationToken ct)
    {
        var url = $"api/v1/{resourceType}/{resourceId}";

        using var getResponse = await _http.GetAsync(url, ct);
        getResponse.EnsureSuccessStatusCode();

        var root = await getResponse.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
        if (root is null) return false;

        var tags = root["tags"]?.AsArray();
        if (tags is null)
        {
            root["tags"] = new JsonArray(tagId);
        }
        else
        {
            if (tags.Any(n => n is not null && n.GetValue<int>() == tagId))
                return false;
            tags.Add(tagId);
        }

        using var putResponse = await _http.PutAsJsonAsync(url, root, cancellationToken: ct);
        putResponse.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<HttpStatusCode> ValidateApiKeyAsync(CancellationToken ct)
    {
        // Authenticated endpoint: requires X-Api-Key, so a bad/stale key returns 401 here
        // (unlike the unauthenticated /api root, which accepts any key).
        using var response = await _http.GetAsync("api/v1/system/status", ct);
        return response.StatusCode;
    }
}
