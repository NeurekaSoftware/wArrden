using wArrden.Clients;
using wArrden.Clients.Models;
using wArrden.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace wArrden.Services;

public class TaggingService
{
    private readonly OutputService _output;
    private readonly IServiceScopeFactory _scopeFactory;

    public TaggingService(OutputService output, IServiceScopeFactory scopeFactory)
    {
        _output = output;
        _scopeFactory = scopeFactory;
    }

    public async Task<int?> FindOrCreateTagAsync(IArrClient client, string tagName, CancellationToken ct)
    {
        var tags = await client.GetTagsAsync(ct);
        var existing = tags.FirstOrDefault(t => string.Equals(t.Label, tagName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _output.WriteDebug($"{client.Instance.ToLowerInvariant()}.tagging", $"Found tag '{tagName}' (ID: {existing.Id})");
            return existing.Id;
        }

        _output.WriteDebug($"{client.Instance.ToLowerInvariant()}.tagging", $"Creating tag '{tagName}'");
        var created = await client.CreateTagAsync(tagName, ct);
        _output.WriteDebug($"{client.Instance.ToLowerInvariant()}.tagging", $"Created tag '{tagName}' (ID: {created.Id})");
        return created.Id;
    }

    public async Task<int> TagEpisodeSeriesAsync(IArrClient client, IReadOnlyList<WantedEpisodeResource> items, int tagId, CancellationToken ct)
    {
        var seriesIds = new HashSet<int>(items.Select(e => e.SeriesId));
        return await TagResourcesAsync(client, "series", seriesIds, tagId, ct);
    }

    public async Task<int> TagSeasonSeriesAsync(IArrClient client, IReadOnlyList<int> seriesIds, int tagId, CancellationToken ct)
    {
        var uniqueIds = new HashSet<int>(seriesIds);
        return await TagResourcesAsync(client, "series", uniqueIds, tagId, ct);
    }

    public async Task<int> TagMoviesAsync(IArrClient client, IReadOnlyList<int> movieIds, int tagId, CancellationToken ct)
    {
        var uniqueIds = new HashSet<int>(movieIds);
        return await TagResourcesAsync(client, "movie", uniqueIds, tagId, ct);
    }

    public async Task<int> TagAlbumArtistsAsync(IArrClient client, IReadOnlyList<WantedAlbumResource> items, int tagId, CancellationToken ct)
    {
        var artistIds = new HashSet<int>();
        foreach (var a in items)
        {
            var artistId = a.Album?.ArtistId ?? a.Artist?.Id ?? 0;
            if (artistId != 0)
                artistIds.Add(artistId);
        }
        return await TagResourcesAsync(client, "artist", artistIds, tagId, ct);
    }

    public async Task<int> TagArtistsAsync(IArrClient client, IReadOnlyList<int> artistIds, int tagId, CancellationToken ct)
    {
        var uniqueIds = new HashSet<int>(artistIds);
        return await TagResourcesAsync(client, "artist", uniqueIds, tagId, ct);
    }

    private async Task<int> TagResourcesAsync(IArrClient client, string resourceType, HashSet<int> resourceIds, int tagId, CancellationToken ct)
    {
        var tagged = 0;
        foreach (var id in resourceIds)
        {
            try
            {
                var applied = resourceType switch
                {
                    "series" => await client.EnsureTagOnSeriesAsync(id, tagId, ct),
                    "movie" => await client.EnsureTagOnMovieAsync(id, tagId, ct),
                    "artist" => await client.EnsureTagOnArtistAsync(id, tagId, ct),
                    _ => false
                };
                if (applied) tagged++;
            }
            catch (Exception ex)
            {
                _output.WriteWarning($"{client.Instance.ToLowerInvariant()}.tagging",
                    $"Failed to tag {resourceType} {id}",
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        return tagged;
    }

    public async Task RunRetroactiveEpisodeAsync(IArrClient client, string instanceName, string category, string tagName, CancellationToken ct)
    {
        var inst = instanceName.ToLowerInvariant();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var entryIds = await db.CooldownEntries
            .Where(e => e.Instance == instanceName && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

        if (entryIds.Count == 0)
        {
            _output.WriteDebug($"{inst}.retrotag", $"No cooldown entries for {category}");
            return;
        }

        _output.WriteDebug($"{inst}.retrotag", $"Resolving {entryIds.Count} episode IDs to series");
        var seriesIds = await client.ResolveSeriesIdsAsync(entryIds.ToArray(), ct);
        _output.WriteDebug($"{inst}.retrotag", $"Resolved {seriesIds.Count} unique series");

        var tagId = await FindOrCreateTagAsync(client, tagName, ct);
        if (tagId is null) return;

        var tagged = await TagResourcesAsync(client, "series", seriesIds, tagId.Value, ct);
        _output.WriteDebug($"{inst}.retrotag", $"Tagged {tagged} series, {seriesIds.Count - tagged} already had tag");
    }

    public async Task RunRetroactiveSeasonAsync(IArrClient client, string instanceName, string category, string tagName, CancellationToken ct)
    {
        var inst = instanceName.ToLowerInvariant();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var seasonKeys = await db.CooldownEntries
            .Where(e => e.Instance == instanceName && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

        if (seasonKeys.Count == 0)
        {
            _output.WriteDebug($"{inst}.retrotag", $"No cooldown entries for {category}");
            return;
        }

        var seriesIds = new HashSet<int>(seasonKeys.Select(k => k / 1000));
        _output.WriteDebug($"{inst}.retrotag", $"Extracted {seriesIds.Count} series from {seasonKeys.Count} season entries");

        var tagId = await FindOrCreateTagAsync(client, tagName, ct);
        if (tagId is null) return;

        var tagged = await TagResourcesAsync(client, "series", seriesIds, tagId.Value, ct);
        _output.WriteDebug($"{inst}.retrotag", $"Tagged {tagged} series, {seriesIds.Count - tagged} already had tag");
    }

    public async Task RunRetroactiveMovieAsync(IArrClient client, string instanceName, string category, string tagName, CancellationToken ct)
    {
        var inst = instanceName.ToLowerInvariant();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var movieIds = await db.CooldownEntries
            .Where(e => e.Instance == instanceName && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

        if (movieIds.Count == 0)
        {
            _output.WriteDebug($"{inst}.retrotag", $"No cooldown entries for {category}");
            return;
        }

        var uniqueIds = new HashSet<int>(movieIds);
        _output.WriteDebug($"{inst}.retrotag", $"Found {movieIds.Count} cooldown entries ({uniqueIds.Count} unique movies)");

        var tagId = await FindOrCreateTagAsync(client, tagName, ct);
        if (tagId is null) return;

        var tagged = await TagResourcesAsync(client, "movie", uniqueIds, tagId.Value, ct);
        _output.WriteDebug($"{inst}.retrotag", $"Tagged {tagged} movies, {uniqueIds.Count - tagged} already had tag");
    }

    public async Task RunRetroactiveAlbumAsync(IArrClient client, string instanceName, string category, string tagName, CancellationToken ct)
    {
        var inst = instanceName.ToLowerInvariant();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var entryIds = await db.CooldownEntries
            .Where(e => e.Instance == instanceName && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

        if (entryIds.Count == 0)
        {
            _output.WriteDebug($"{inst}.retrotag", $"No cooldown entries for {category}");
            return;
        }

        _output.WriteDebug($"{inst}.retrotag", $"Resolving {entryIds.Count} album IDs to artists");
        var artistIds = await client.ResolveArtistIdsAsync(entryIds.ToArray(), ct);
        _output.WriteDebug($"{inst}.retrotag", $"Resolved {artistIds.Count} unique artists");

        var tagId = await FindOrCreateTagAsync(client, tagName, ct);
        if (tagId is null) return;

        var tagged = await TagResourcesAsync(client, "artist", artistIds, tagId.Value, ct);
        _output.WriteDebug($"{inst}.retrotag", $"Tagged {tagged} artists, {artistIds.Count - tagged} already had tag");
    }

    public async Task RunRetroactiveArtistAsync(IArrClient client, string instanceName, string category, string tagName, CancellationToken ct)
    {
        var inst = instanceName.ToLowerInvariant();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var artistIds = await db.CooldownEntries
            .Where(e => e.Instance == instanceName && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

        if (artistIds.Count == 0)
        {
            _output.WriteDebug($"{inst}.retrotag", $"No cooldown entries for {category}");
            return;
        }

        var uniqueIds = new HashSet<int>(artistIds);
        _output.WriteDebug($"{inst}.retrotag", $"Found {artistIds.Count} cooldown entries ({uniqueIds.Count} unique artists)");

        var tagId = await FindOrCreateTagAsync(client, tagName, ct);
        if (tagId is null) return;

        var tagged = await TagResourcesAsync(client, "artist", uniqueIds, tagId.Value, ct);
        _output.WriteDebug($"{inst}.retrotag", $"Tagged {tagged} artists, {uniqueIds.Count - tagged} already had tag");
    }
}
