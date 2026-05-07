using System.Net.Http.Json;
using System.Text.Json;
using ArrWarden.Clients.Models;

namespace ArrWarden.Clients;

public class RadarrV3Client : IArrClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public string Instance => "Radarr";

    public RadarrV3Client(string url, string apiKey)
    {
        _baseUrl = url.TrimEnd('/');
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task<IReadOnlyList<QueueResource>> GetQueueAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync($"{_baseUrl}/api/v3/queue?includeUnknownMovieItems=true", ct);
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

    public Task<IReadOnlyList<WantedEpisodeResource>> GetWantedMissingEpisodesAsync(CancellationToken ct)
        => throw new NotSupportedException("Radarr does not support episode wanted endpoints.");

    public Task<IReadOnlyList<WantedEpisodeResource>> GetWantedCutoffEpisodesAsync(CancellationToken ct)
        => throw new NotSupportedException("Radarr does not support episode wanted endpoints.");

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
        var all = new List<WantedMovieResource>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var url = $"{_baseUrl}/api/v3/wanted/{type}?monitored=true&page={page}&pageSize={pageSize}&sortKey=lastSearchTime&sortDirection=ascending";
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var paging = await response.Content.ReadFromJsonAsync<WantedPagingResource<WantedMovieResource>>(cancellationToken: ct);
            if (paging?.Records is { Count: > 0 })
                all.AddRange(paging.Records);

            if (paging == null || all.Count >= paging.TotalRecords)
                break;

            page++;
        }

        return all.Where(m => m.Monitored).ToList();
    }

    public Task TriggerEpisodeSearchAsync(int[] episodeIds, CancellationToken ct)
        => throw new NotSupportedException("Radarr does not support episode search.");

    public async Task TriggerMoviesSearchAsync(int[] movieIds, CancellationToken ct)
    {
        var body = new { name = "MoviesSearch", movieIds };
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v3/command", body, cancellationToken: ct);
        response.EnsureSuccessStatusCode();
    }
}
