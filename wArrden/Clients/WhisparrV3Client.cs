using System.Net.Http.Json;
using System.Text.Json.Nodes;
using wArrden.Clients.Models;

namespace wArrden.Clients;

public class WhisparrV3Client : IArrClient
{
    private readonly HttpClient _http;
    private bool _disposed;

    public string Instance { get; }

    public WhisparrV3Client(HttpClient http, string instanceName)
    {
        Instance = instanceName;
        _http = http;
    }

    // Test seam: wraps the supplied handler in an HttpClient carrying the same base address
    // and API-key header that the factory-configured client uses in production.
    internal WhisparrV3Client(string url, string apiKey, string instanceName, HttpMessageHandler handler)
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
            var url = $"api/v3/queue?includeUnknownSeriesItems=true&includeSeries=true&includeEpisode=true&page={page}&pageSize={pageSize}";
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
            $"api/v3/queue/{queueId}?blocklist=true&skipRedownload=false", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQueueItemWithoutBlocklistAsync(int queueId, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"api/v3/queue/{queueId}?blocklist=false&skipRedownload=false", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<WantedEpisodeResource>> GetWantedMissingEpisodesAsync(CancellationToken ct)
    {
        return await FetchAllWantedPagesAsync("missing", ct);
    }

    public async Task<IReadOnlyList<WantedEpisodeResource>> GetWantedCutoffEpisodesAsync(CancellationToken ct)
    {
        return await FetchAllWantedPagesAsync("cutoff", ct);
    }

    private async Task<IReadOnlyList<WantedEpisodeResource>> FetchAllWantedPagesAsync(string type, CancellationToken ct)
    {
        var byId = new Dictionary<int, WantedEpisodeResource>(capacity: 100);
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"api/v3/wanted/{type}?includeSeries=true&monitored=true&page={page}&pageSize={pageSize}&sortKey=episodes.lastSearchTime&sortDirection=ascending";
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.WantedPagingResourceWantedEpisodeResource, ct);
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

    public Task TriggerEpisodeSearchAsync(int[] episodeIds, CancellationToken ct)
    {
        var body = new { name = "EpisodeSearch", episodeIds };
        return PostCommandAsync(body, ct);
    }

    public Task TriggerSeasonSearchAsync(int seriesId, int seasonNumber, CancellationToken ct)
    {
        var body = new { name = "SeasonSearch", seriesId, seasonNumber };
        return PostCommandAsync(body, ct);
    }

    private async Task PostCommandAsync(object command, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync($"api/v3/command", command, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
    }

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedMissingMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support movie wanted endpoints.");

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedCutoffMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support movie wanted endpoints.");

    public async Task<IReadOnlyList<IndexerResource>> GetIndexersAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync($"api/v3/indexer", ct);
        response.EnsureSuccessStatusCode();
        return (IReadOnlyList<IndexerResource>?)await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.IndexerResourceArray, ct) ?? Array.Empty<IndexerResource>();
    }

    public async Task<bool> HasAnyEnabledIndexerAsync(CancellationToken ct)
    {
        var indexers = await GetIndexersAsync(ct);
        return indexers.Any(i => i.EnableAutomaticSearch);
    }

    Task IArrClient.TriggerMoviesSearchAsync(int[] movieIds, CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support movie search.");

    Task<IReadOnlyList<WantedAlbumResource>> IArrClient.GetWantedMissingAlbumsAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support album wanted endpoints.");

    Task<IReadOnlyList<WantedAlbumResource>> IArrClient.GetWantedCutoffAlbumsAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support album wanted endpoints.");

    Task IArrClient.TriggerAlbumSearchAsync(int[] albumIds, CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support album search.");

    Task IArrClient.TriggerArtistSearchAsync(int artistId, CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support artist search.");

    public async Task<IReadOnlyList<TagResource>> GetTagsAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync($"api/v3/tag", ct);
        response.EnsureSuccessStatusCode();
        return (IReadOnlyList<TagResource>?)await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.ListTagResource, ct) ?? Array.Empty<TagResource>();
    }

    public async Task<TagResource> CreateTagAsync(string label, CancellationToken ct)
    {
        var body = JsonContent.Create(new { label });
        using var response = await _http.PostAsync($"api/v3/tag", body, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.TagResource, ct))!;
    }

    public async Task<bool> EnsureTagOnSeriesAsync(int seriesId, int tagId, CancellationToken ct)
    {
        return await EnsureTagOnResourceAsync("series", seriesId, tagId, ct);
    }

    public Task<bool> EnsureTagOnMovieAsync(int movieId, int tagId, CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support movie tags.");

    public Task<bool> EnsureTagOnArtistAsync(int artistId, int tagId, CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support artist tags.");

    public async Task<HashSet<int>> ResolveSeriesIdsAsync(int[] episodeIds, CancellationToken ct)
    {
        var seriesIds = new HashSet<int>();
        const int batchSize = 50;
        for (var i = 0; i < episodeIds.Length; i += batchSize)
        {
            var batch = episodeIds.Skip(i).Take(batchSize);
            var idsParam = string.Join("&episodeIds=", batch);
            using var response = await _http.GetAsync(
                $"api/v3/episode?episodeIds={idsParam}&includeSeries=true", ct);
            response.EnsureSuccessStatusCode();

            var root = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
            if (root is null) continue;

            foreach (var ep in root.AsArray())
            {
                var sid = ep?["seriesId"]?.GetValue<int>();
                if (sid.HasValue && sid.Value != 0)
                    seriesIds.Add(sid.Value);
            }
        }
        return seriesIds;
    }

    public Task<HashSet<int>> ResolveArtistIdsAsync(int[] albumIds, CancellationToken ct)
        => throw new NotSupportedException("Whisparr does not support artist resolution.");

    private async Task<bool> EnsureTagOnResourceAsync(string resourceType, int resourceId, int tagId, CancellationToken ct)
    {
        var url = $"api/v3/{resourceType}/{resourceId}";

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

    public async Task<bool> ValidateApiKeyAsync(CancellationToken ct)
    {
        // Authenticated endpoint: requires X-Api-Key, so a bad/stale key returns 401 here
        // (unlike the unauthenticated /api root, which accepts any key).
        using var response = await _http.GetAsync("api/v3/system/status", ct);
        return response.IsSuccessStatusCode;
    }
}
