using System.Net.Http.Json;
using System.Text.Json;
using ArrWarden.Clients.Models;

namespace ArrWarden.Clients;

public class SonarrV3Client : IArrClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public string Instance => "Sonarr";

    public SonarrV3Client(string url, string apiKey)
    {
        _baseUrl = url.TrimEnd('/');
        _http = new HttpClient();
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
            var url = $"{_baseUrl}/api/v3/wanted/{type}?includeSeries=false&monitored=true&page={page}&pageSize={pageSize}&sortKey=episodes.lastSearchTime&sortDirection=ascending";
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

    private async Task PostCommandAsync(object command, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v3/command", command, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
    }

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedMissingMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support movie wanted endpoints.");

    Task<IReadOnlyList<WantedMovieResource>> IArrClient.GetWantedCutoffMoviesAsync(CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support movie wanted endpoints.");

    Task IArrClient.TriggerMoviesSearchAsync(int[] movieIds, CancellationToken ct)
        => throw new NotSupportedException("Sonarr does not support movie search.");
}
