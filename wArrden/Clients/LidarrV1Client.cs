using System.Net.Http.Json;
using System.Text.Json.Nodes;
using wArrden.Clients.Models;

namespace wArrden.Clients;

public class LidarrV1Client : IArrClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public string Instance { get; }

    public LidarrV1Client(string url, string apiKey, string instanceName)
        : this(url, apiKey, instanceName, new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) })
    {
    }

    internal LidarrV1Client(string url, string apiKey, string instanceName, HttpMessageHandler handler)
    {
        Instance = instanceName;
        _baseUrl = url.TrimEnd('/');
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    public async Task<IReadOnlyList<QueueResource>> GetQueueAsync(CancellationToken ct)
    {
        var all = new List<QueueResource>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"{_baseUrl}/api/v1/queue?includeUnknownArtistItems=true&includeArtist=true&includeAlbum=true&page={page}&pageSize={pageSize}";
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.WantedPagingResourceQueueResource, ct);
            if (paging?.Records is { Count: > 0 })
                all.AddRange(paging.Records);

            if (paging == null || all.Count >= paging.TotalRecords)
                break;

            page++;
        }

        return all;
    }

    public async Task DeleteQueueItemAsync(int queueId, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"{_baseUrl}/api/v1/queue/{queueId}?blocklist=true&removeFromClient=true", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQueueItemWithoutBlocklistAsync(int queueId, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"{_baseUrl}/api/v1/queue/{queueId}?blocklist=false&removeFromClient=true", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<IndexerResource>> GetIndexersAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync($"{_baseUrl}/api/v1/indexer", ct);
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
        var all = new List<WantedAlbumResource>(capacity: 100);
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"{_baseUrl}/api/v1/wanted/{type}?includeArtist=true&monitored=true&page={page}&pageSize={pageSize}&sortKey=albums.lastSearchTime&sortDirection=ascending";
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.WantedPagingResourceWantedAlbumResource, ct);
            if (paging?.Records is { Count: > 0 })
                all.AddRange(paging.Records);

            if (paging == null || all.Count >= paging.TotalRecords)
                break;

            page++;
        }

        return all;
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
        using var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/command", command, cancellationToken: ct);
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
        using var response = await _http.GetAsync($"{_baseUrl}/api/v1/tag", ct);
        response.EnsureSuccessStatusCode();
        return (IReadOnlyList<TagResource>?)await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.ListTagResource, ct) ?? Array.Empty<TagResource>();
    }

    public async Task<TagResource> CreateTagAsync(string label, CancellationToken ct)
    {
        var body = JsonContent.Create(new { label });
        using var response = await _http.PostAsync($"{_baseUrl}/api/v1/tag", body, ct);
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
                $"{_baseUrl}/api/v1/album?albumIds={idsParam}", ct);
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
        var url = $"{_baseUrl}/api/v1/{resourceType}/{resourceId}";

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
        try
        {
            using var response = await _http.GetAsync($"{_baseUrl}/api", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
