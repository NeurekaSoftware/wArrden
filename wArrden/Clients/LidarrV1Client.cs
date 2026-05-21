using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task<IReadOnlyList<QueueResource>> GetQueueAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync(
            $"{_baseUrl}/api/v1/queue?includeUnknownArtistItems=true&includeArtist=true&includeAlbum=true", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var records = doc.RootElement.GetProperty("records");
        return JsonSerializer.Deserialize<List<QueueResource>>(records.GetRawText()) ?? new();
    }

    public async Task DeleteQueueItemAsync(int queueId, CancellationToken ct)
    {
        var response = await _http.DeleteAsync(
            $"{_baseUrl}/api/v1/queue/{queueId}?blocklist=true&removeFromClient=true", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQueueItemWithoutBlocklistAsync(int queueId, CancellationToken ct)
    {
        var response = await _http.DeleteAsync(
            $"{_baseUrl}/api/v1/queue/{queueId}?blocklist=false&removeFromClient=true", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<IndexerResource>> GetIndexersAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync($"{_baseUrl}/api/v1/indexer", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<IndexerResource>>(cancellationToken: ct) ?? new();
    }

    public async Task<bool> HasAnyEnabledIndexerAsync(CancellationToken ct)
    {
        var indexers = await GetIndexersAsync(ct);
        return indexers.Any(i => i.Enable);
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
