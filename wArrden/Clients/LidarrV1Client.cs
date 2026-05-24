using System.Net.Http.Json;
using wArrden.Clients.Models;

namespace wArrden.Clients;

public class LidarrV1Client : IArrClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public string Instance { get; }

    public LidarrV1Client(string url, string apiKey, string instanceName)
        : this(url, apiKey, instanceName, new HttpClientHandler())
    {
    }

    internal LidarrV1Client(string url, string apiKey, string instanceName, HttpMessageHandler handler)
    {
        Instance = instanceName;
        _baseUrl = url.TrimEnd('/');
        _http = new HttpClient(handler);
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    public async Task<IReadOnlyList<QueueResource>> GetQueueAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync(
            $"{_baseUrl}/api/v1/queue?includeUnknownArtistItems=true&includeArtist=true&includeAlbum=true", ct);
        response.EnsureSuccessStatusCode();
        var paging = await response.Content.ReadFromJsonAsync<WantedPagingResource<QueueResource>>(cancellationToken: ct);
        return paging?.Records ?? new();
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
        return await response.Content.ReadFromJsonAsync<List<IndexerResource>>(cancellationToken: ct) ?? new();
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
        var all = new List<WantedAlbumResource>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"{_baseUrl}/api/v1/wanted/{type}?includeArtist=true&monitored=true&page={page}&pageSize={pageSize}&sortKey=albums.lastSearchTime&sortDirection=ascending";
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync<WantedPagingResource<WantedAlbumResource>>(cancellationToken: ct);
            if (paging?.Records is { Count: > 0 })
                all.AddRange(paging.Records);

            if (paging == null || all.Count >= paging.TotalRecords)
                break;

            page++;
        }

        all.RemoveAll(a => !a.Monitored);
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
}
