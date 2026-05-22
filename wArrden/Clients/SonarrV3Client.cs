using System.Net.Http.Json;
using System.Text.Json;
using wArrden.Clients.Models;

namespace wArrden.Clients;

public class SonarrV3Client : IArrClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public string Instance { get; }

    public SonarrV3Client(string url, string apiKey, string instanceName)
        : this(url, apiKey, instanceName, new HttpClientHandler())
    {
    }

    internal SonarrV3Client(string url, string apiKey, string instanceName, HttpMessageHandler handler)
    {
        Instance = instanceName;
        _baseUrl = url.TrimEnd('/');
        _http = new HttpClient(handler);
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task<IReadOnlyList<QueueResource>> GetQueueAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync($"{_baseUrl}/api/v3/queue?includeUnknownSeriesItems=true", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var records = doc.RootElement.GetProperty("records");
        return JsonSerializer.Deserialize<List<QueueResource>>(records.GetRawText()) ?? new();
    }

    public async Task DeleteQueueItemAsync(int queueId, CancellationToken ct)
    {
        var response = await _http.DeleteAsync($"{_baseUrl}/api/v3/queue/{queueId}?blocklist=true&skipRedownload=false", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQueueItemWithoutBlocklistAsync(int queueId, CancellationToken ct)
    {
        var response = await _http.DeleteAsync($"{_baseUrl}/api/v3/queue/{queueId}?blocklist=false&skipRedownload=false", ct);
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
        var all = new List<WantedEpisodeResource>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"{_baseUrl}/api/v3/wanted/{type}?includeSeries=true&monitored=true&page={page}&pageSize={pageSize}&sortKey=episodes.lastSearchTime&sortDirection=ascending";
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync<WantedPagingResource<WantedEpisodeResource>>(cancellationToken: ct);
            if (paging?.Records is { Count: > 0 })
                all.AddRange(paging.Records);

            if (paging == null || all.Count >= paging.TotalRecords)
                break;

            page++;
        }

        return all.Where(e => e.Monitored).ToList();
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
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v3/command", command, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
    }

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedMissingMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support movie wanted endpoints.");

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedCutoffMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support movie wanted endpoints.");

    public async Task<IReadOnlyList<IndexerResource>> GetIndexersAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync($"{_baseUrl}/api/v3/indexer", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<IndexerResource>>(cancellationToken: ct) ?? new();
    }

    public async Task<bool> HasAnyEnabledIndexerAsync(CancellationToken ct)
    {
        var indexers = await GetIndexersAsync(ct);
        return indexers.Any(i => i.Enable);
    }

    Task IArrClient.TriggerMoviesSearchAsync(int[] movieIds, CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support movie search.");

    Task<IReadOnlyList<WantedAlbumResource>> IArrClient.GetWantedMissingAlbumsAsync(CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support album wanted endpoints.");

    Task<IReadOnlyList<WantedAlbumResource>> IArrClient.GetWantedCutoffAlbumsAsync(CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support album wanted endpoints.");

    Task IArrClient.TriggerAlbumSearchAsync(int[] albumIds, CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support album search.");

    Task IArrClient.TriggerArtistSearchAsync(int artistId, CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support artist search.");
}
