using System.Net.Http.Json;
using wArrden.Clients.Models;

namespace wArrden.Clients;

public class WhisparrV3ErosClient : IArrClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public string Instance { get; }

    public WhisparrV3ErosClient(string url, string apiKey, string instanceName)
        : this(url, apiKey, instanceName, new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) })
    {
    }

    internal WhisparrV3ErosClient(string url, string apiKey, string instanceName, HttpMessageHandler handler)
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
        using var response = await _http.GetAsync(
            $"{_baseUrl}/api/v3/queue?includeUnknownMovieItems=true&includeMovie=true", ct);
        response.EnsureSuccessStatusCode();
        var paging = await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.WantedPagingResourceQueueResource, ct);
        return (IReadOnlyList<QueueResource>?)paging?.Records ?? Array.Empty<QueueResource>();
    }

    public async Task DeleteQueueItemAsync(int queueId, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"{_baseUrl}/api/v3/queue/{queueId}?removeFromClient=true&blocklist=true&skipRedownload=false", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQueueItemWithoutBlocklistAsync(int queueId, CancellationToken ct)
    {
        using var response = await _http.DeleteAsync(
            $"{_baseUrl}/api/v3/queue/{queueId}?removeFromClient=true&blocklist=false&skipRedownload=false", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<WantedMovieResource>> GetWantedMissingMoviesAsync(CancellationToken ct)
    {
        return await FetchAllWantedPagesAsync("missing", ct);
    }

    public async Task<IReadOnlyList<WantedMovieResource>> GetWantedCutoffMoviesAsync(CancellationToken ct)
    {
        return await FetchAllWantedPagesAsync("cutoff", ct);
    }

    private async Task<IReadOnlyList<WantedMovieResource>> FetchAllWantedPagesAsync(string type, CancellationToken ct)
    {
        var all = new List<WantedMovieResource>(capacity: 100);
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"{_baseUrl}/api/v3/wanted/{type}?monitored=true&page={page}&pageSize={pageSize}&sortKey=movies.lastSearchTime&sortDirection=ascending";
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.WantedPagingResourceWantedMovieResource, ct);
            if (paging?.Records is { Count: > 0 })
                all.AddRange(paging.Records);

            if (paging == null || all.Count >= paging.TotalRecords)
                break;

            page++;
        }

        return all;
    }

    public async Task TriggerMoviesSearchAsync(int[] movieIds, CancellationToken ct)
    {
        var body = new { name = "MoviesSearch", movieIds };
        using var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v3/command", body, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<IndexerResource>> GetIndexersAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync($"{_baseUrl}/api/v3/indexer", ct);
        response.EnsureSuccessStatusCode();
        return (IReadOnlyList<IndexerResource>?)await response.Content.ReadFromJsonAsync(ArrJsonContext.Default.IndexerResourceArray, ct) ?? Array.Empty<IndexerResource>();
    }

    public async Task<bool> HasAnyEnabledIndexerAsync(CancellationToken ct)
    {
        var indexers = await GetIndexersAsync(ct);
        return indexers.Any(i => i.EnableAutomaticSearch);
    }

    Task<IReadOnlyList<WantedEpisodeResource>> IArrClient.GetWantedMissingEpisodesAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support episode wanted endpoints.");

    Task<IReadOnlyList<WantedEpisodeResource>> IArrClient.GetWantedCutoffEpisodesAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support episode wanted endpoints.");

    Task IArrClient.TriggerEpisodeSearchAsync(int[] episodeIds, CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support episode search.");

    Task IArrClient.TriggerSeasonSearchAsync(int seriesId, int seasonNumber, CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support season search.");

    Task<IReadOnlyList<WantedAlbumResource>> IArrClient.GetWantedMissingAlbumsAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support album wanted endpoints.");

    Task<IReadOnlyList<WantedAlbumResource>> IArrClient.GetWantedCutoffAlbumsAsync(CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support album wanted endpoints.");

    Task IArrClient.TriggerAlbumSearchAsync(int[] albumIds, CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support album search.");

    Task IArrClient.TriggerArtistSearchAsync(int artistId, CancellationToken ct)
        => throw new NotSupportedException("Whisparr v3 Eros does not support artist search.");

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
