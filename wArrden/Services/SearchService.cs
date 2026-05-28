using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Services;

public class SearchService
{
    private readonly ICooldownService _cooldown;
    private readonly OutputService _output;

    public SearchService(ICooldownService cooldown, OutputService output)
    {
        _cooldown = cooldown;
        _output = output;
    }

    public virtual async Task SearchMissingEpisodesAsync(IArrClient client, int maxResults, TimeSpan cooldown, string searchType, bool isDryRun, IReadOnlyList<string>? indexerNames, CancellationToken ct)
    {
        var progress = _output.CreateSearchWriter(client.Instance, "Missing Search", maxResults);
        progress.WriteHeader();
        if (string.Equals(searchType, "season", StringComparison.OrdinalIgnoreCase))
        {
            await RunSeasonSearch(client, "Missing", cooldown, maxResults, isDryRun,
                () => client.GetWantedMissingEpisodesAsync(ct), indexerNames, progress, ct);
        }
        else
        {
            await RunEpisodeSearch(client, "Missing", cooldown, maxResults, isDryRun,
                () => client.GetWantedMissingEpisodesAsync(ct), async ids => await client.TriggerEpisodeSearchAsync(ids, ct), indexerNames, progress, ct);
        }
    }

    public virtual async Task SearchUpgradeEpisodesAsync(IArrClient client, int maxResults, TimeSpan cooldown, string searchType, bool isDryRun, IReadOnlyList<string>? indexerNames, CancellationToken ct)
    {
        var progress = _output.CreateSearchWriter(client.Instance, "Upgrade Search", maxResults);
        progress.WriteHeader();
        if (string.Equals(searchType, "season", StringComparison.OrdinalIgnoreCase))
        {
            await RunSeasonSearch(client, "Upgrade", cooldown, maxResults, isDryRun,
                () => client.GetWantedCutoffEpisodesAsync(ct), indexerNames, progress, ct);
        }
        else
        {
            await RunEpisodeSearch(client, "Upgrade", cooldown, maxResults, isDryRun,
                () => client.GetWantedCutoffEpisodesAsync(ct), async ids => await client.TriggerEpisodeSearchAsync(ids, ct), indexerNames, progress, ct);
        }
    }

    public virtual async Task SearchMissingMoviesAsync(IArrClient client, int maxResults, TimeSpan cooldown, bool isDryRun, IReadOnlyList<string>? indexerNames, CancellationToken ct)
    {
        var progress = _output.CreateSearchWriter(client.Instance, "Missing Search", maxResults);
        progress.WriteHeader();
        await RunMovieSearch(client, "Missing", cooldown, maxResults, isDryRun,
            () => client.GetWantedMissingMoviesAsync(ct), async ids => await client.TriggerMoviesSearchAsync(ids, ct), indexerNames, progress, ct);
    }

    public virtual async Task SearchUpgradeMoviesAsync(IArrClient client, int maxResults, TimeSpan cooldown, bool isDryRun, IReadOnlyList<string>? indexerNames, CancellationToken ct)
    {
        var progress = _output.CreateSearchWriter(client.Instance, "Upgrade Search", maxResults);
        progress.WriteHeader();
        await RunMovieSearch(client, "Upgrade", cooldown, maxResults, isDryRun,
            () => client.GetWantedCutoffMoviesAsync(ct), async ids => await client.TriggerMoviesSearchAsync(ids, ct), indexerNames, progress, ct);
    }

    public virtual async Task SearchMissingAlbumsAsync(IArrClient client, int maxResults, TimeSpan cooldown, string searchType, bool isDryRun, IReadOnlyList<string>? indexerNames, CancellationToken ct)
    {
        var progress = _output.CreateSearchWriter(client.Instance, "Missing Search", maxResults);
        progress.WriteHeader();
        if (string.Equals(searchType, "artist", StringComparison.OrdinalIgnoreCase))
        {
            await RunArtistSearch(client, "Missing", cooldown, maxResults, isDryRun,
                () => client.GetWantedMissingAlbumsAsync(ct), indexerNames, progress, ct);
        }
        else
        {
            await RunAlbumSearch(client, "Missing", cooldown, maxResults, isDryRun,
                () => client.GetWantedMissingAlbumsAsync(ct), async ids => await client.TriggerAlbumSearchAsync(ids, ct), indexerNames, progress, ct);
        }
    }

    public virtual async Task SearchUpgradeAlbumsAsync(IArrClient client, int maxResults, TimeSpan cooldown, string searchType, bool isDryRun, IReadOnlyList<string>? indexerNames, CancellationToken ct)
    {
        var progress = _output.CreateSearchWriter(client.Instance, "Upgrade Search", maxResults);
        progress.WriteHeader();
        if (string.Equals(searchType, "artist", StringComparison.OrdinalIgnoreCase))
        {
            await RunArtistSearch(client, "Upgrade", cooldown, maxResults, isDryRun,
                () => client.GetWantedCutoffAlbumsAsync(ct), indexerNames, progress, ct);
        }
        else
        {
            await RunAlbumSearch(client, "Upgrade", cooldown, maxResults, isDryRun,
                () => client.GetWantedCutoffAlbumsAsync(ct), async ids => await client.TriggerAlbumSearchAsync(ids, ct), indexerNames, progress, ct);
        }
    }

    private async Task RunEpisodeSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedEpisodeResource>>> getWanted, Func<int[], Task> triggerSearch,
        IReadOnlyList<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
        var inst = client.Instance.ToLowerInvariant();

        progress.SetPhase("Cleaning cooldown entries");
        await _cooldown.CleanExpiredAsync(client.Instance, category, cooldown, ct);

        progress.SetPhase("Fetching wanted episodes");
        var wanted = await getWanted();

        _output.WriteDebug($"{inst}.missing", $"Fetched {wanted.Count} wanted episodes");

        if (wanted.Count == 0)
        {
            progress.WriteStats(0, 0, 0, 0, true);
            return;
        }

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, category, ct);

        var eligible = new List<WantedEpisodeResource>(wanted.Count);
        foreach (var e in wanted)
            if (!cooldownIds.Contains(e.Id))
                eligible.Add(e);
        Shuffle(eligible, maxResults);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(e => e.Series?.Title ?? "")
            .ThenBy(e => e.SeasonNumber)
            .ThenBy(e => e.EpisodeNumber)
            .ToList();
        var onCooldown = wanted.Count - eligible.Count;

        _output.WriteDebug($"{inst}.missing",
            $"Cooldown filter: {onCooldown} on cooldown, {eligible.Count} eligible, {selected.Count} selected");

        var eligibleCount = eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(wanted.Count, onCooldown, eligibleCount,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            _output.WriteWarning($"{inst}.missing", "No enabled indexers — search skipped",
                indexerNames is { Count: > 0 } ? $"Configured indexers: {string.Join(", ", indexerNames)}" : "No automatic-search indexers found");
            progress.WriteStats(wanted.Count, onCooldown, eligibleCount, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} items");
        progress.WriteStats(wanted.Count, onCooldown, eligibleCount, selected.Count, false);
        progress.StartResults();

        foreach (var ep in selected)
        {
            var title = ep.Series is not null
                ? $"{ep.Series.Title} ({ep.Series.Year}) - S{ep.SeasonNumber:D2}E{ep.EpisodeNumber:D2} - {ep.Title ?? $"Episode {ep.Id}"}"
                : ep.Title ?? $"Episode {ep.Id}";

            progress.WriteItem(title);
        }

        var ids = selected.Select(s => s.Id).ToArray();
        try { await triggerSearch(ids); }
        catch (Exception ex)
        {
            var titles = selected.Select(ep =>
                ep.Series is not null
                    ? $"{ep.Series.Title} ({ep.Series.Year}) - S{ep.SeasonNumber:D2}E{ep.EpisodeNumber:D2} - {ep.Title ?? $"Episode {ep.Id}"}"
                    : ep.Title ?? $"Episode {ep.Id}");
            _output.WriteWarning($"{inst}.missing",
                $"Search trigger failed for {string.Join(", ", titles)}", ex.Message);
        }

        await _cooldown.MarkSearchedAsync(client.Instance, category, ids, ct);
        progress.WriteTrailer();
    }

    private async Task RunSeasonSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedEpisodeResource>>> getWanted,
        IReadOnlyList<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
        var inst = client.Instance.ToLowerInvariant();
        var seasonCategory = $"{category}_Season";

        progress.SetPhase("Cleaning cooldown entries");
        await _cooldown.CleanExpiredAsync(client.Instance, seasonCategory, cooldown, ct);

        progress.SetPhase("Fetching wanted episodes");
        var wanted = await getWanted();

        if (wanted.Count == 0)
        {
            progress.WriteStats(0, 0, 0, 0, true);
            return;
        }

        progress.SetPhase("Grouping by season");
        var seasons = wanted
            .GroupBy(e => (e.SeriesId, e.SeasonNumber))
            .Select(g => new SeasonGroup(
                g.Key.SeriesId,
                g.Key.SeasonNumber,
                g.First().Series,
                g.Key.SeriesId * 1000 + g.Key.SeasonNumber
            ))
            .ToList();

        _output.WriteDebug($"{inst}.missing", $"Grouped {wanted.Count} episodes into {seasons.Count} seasons");

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, seasonCategory, ct);

        var eligible = new List<SeasonGroup>(seasons.Count);
        foreach (var s in seasons)
            if (!cooldownIds.Contains(s.SeasonKey))
                eligible.Add(s);
        Shuffle(eligible, maxResults);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(s => s.Series?.Title ?? "")
            .ThenBy(s => s.SeasonNumber)
            .ToList();
        var onCooldown = seasons.Count - eligible.Count;

        _output.WriteDebug($"{inst}.missing",
            $"Season cooldown filter: {onCooldown} on cooldown, {eligible.Count} eligible, {selected.Count} selected");

        var eligibleCount = eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(seasons.Count, onCooldown, eligibleCount,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            _output.WriteWarning($"{inst}.missing", "No enabled indexers — search skipped",
                indexerNames is { Count: > 0 } ? $"Configured indexers: {string.Join(", ", indexerNames)}" : "No automatic-search indexers found");
            progress.WriteStats(seasons.Count, onCooldown, eligibleCount, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} seasons");
        progress.WriteStats(seasons.Count, onCooldown, eligibleCount, selected.Count, false);
        progress.StartResults();

        foreach (var s in selected)
        {
            var seriesName = s.Series?.Title ?? $"Series {s.SeriesId}";
            var title = s.Series is not null && s.Series.Year > 0
                ? $"{seriesName} ({s.Series.Year}) - Season {s.SeasonNumber}"
                : $"{seriesName} - Season {s.SeasonNumber}";

            progress.WriteItem(title);

            try { await client.TriggerSeasonSearchAsync(s.SeriesId, s.SeasonNumber, ct); }
            catch (Exception ex)
            {
                _output.WriteWarning($"{inst}.missing",
                    $"Search trigger failed for {title}", ex.Message);
            }
        }

        var seasonKeys = selected.Select(s => s.SeasonKey).ToArray();
        await _cooldown.MarkSearchedAsync(client.Instance, seasonCategory, seasonKeys, ct);
        progress.WriteTrailer();
    }

    private readonly record struct SeasonGroup(int SeriesId, int SeasonNumber, WantedEpisodeSeriesResource? Series, int SeasonKey);

    private async Task RunAlbumSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedAlbumResource>>> getWanted, Func<int[], Task> triggerSearch,
        IReadOnlyList<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
        var inst = client.Instance.ToLowerInvariant();

        progress.SetPhase("Cleaning cooldown entries");
        await _cooldown.CleanExpiredAsync(client.Instance, category, cooldown, ct);

        progress.SetPhase("Fetching wanted albums");
        var wanted = await getWanted();

        _output.WriteDebug($"{inst}.missing", $"Fetched {wanted.Count} wanted albums");

        if (wanted.Count == 0)
        {
            progress.WriteStats(0, 0, 0, 0, true);
            return;
        }

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, category, ct);

        var eligible = new List<WantedAlbumResource>(wanted.Count);
        foreach (var a in wanted)
            if (!cooldownIds.Contains(a.Id))
                eligible.Add(a);
        Shuffle(eligible, maxResults);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(a => a.Artist?.ArtistName ?? "")
            .ThenBy(a => a.Album?.Title ?? "")
            .ToList();
        var onCooldown = wanted.Count - eligible.Count;

        _output.WriteDebug($"{inst}.missing",
            $"Cooldown filter: {onCooldown} on cooldown, {eligible.Count} eligible, {selected.Count} selected");

        var eligibleCount = eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(wanted.Count, onCooldown, eligibleCount,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            _output.WriteWarning($"{inst}.missing", "No enabled indexers — search skipped",
                indexerNames is { Count: > 0 } ? $"Configured indexers: {string.Join(", ", indexerNames)}" : "No automatic-search indexers found");
            progress.WriteStats(wanted.Count, onCooldown, eligibleCount, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} items");
        progress.WriteStats(wanted.Count, onCooldown, eligibleCount, selected.Count, false);
        progress.StartResults();

        foreach (var a in selected)
        {
            var title = $"{a.Artist?.ArtistName ?? "Artist Unknown"} - {a.Album?.Title ?? $"Album {a.Id}"}";

            progress.WriteItem(title);
        }

        var ids = selected.Select(s => s.Id).ToArray();
        try { await triggerSearch(ids); }
        catch (Exception ex)
        {
            var titles = selected.Select(a =>
                $"{a.Artist?.ArtistName ?? "Artist Unknown"} - {a.Album?.Title ?? $"Album {a.Id}"}");
            _output.WriteWarning($"{inst}.missing",
                $"Search trigger failed for {string.Join(", ", titles)}", ex.Message);
        }

        await _cooldown.MarkSearchedAsync(client.Instance, category, ids, ct);
        progress.WriteTrailer();
    }

    private async Task RunArtistSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedAlbumResource>>> getWanted,
        IReadOnlyList<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
        var inst = client.Instance.ToLowerInvariant();
        var artistCategory = $"{category}_Artist";

        progress.SetPhase("Cleaning cooldown entries");
        await _cooldown.CleanExpiredAsync(client.Instance, artistCategory, cooldown, ct);

        progress.SetPhase("Fetching wanted albums");
        var wanted = await getWanted();

        if (wanted.Count == 0)
        {
            progress.WriteStats(0, 0, 0, 0, true);
            return;
        }

        progress.SetPhase("Grouping by artist");
        var artists = wanted
            .GroupBy(a => a.Album?.ArtistId ?? a.Artist?.Id ?? 0)
            .Where(g => g.Key != 0)
            .Select(g => new ArtistGroup(
                g.Key,
                g.First().Artist
            ))
            .ToList();

        _output.WriteDebug($"{inst}.missing", $"Grouped {wanted.Count} albums into {artists.Count} artists");

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, artistCategory, ct);

        var eligible = new List<ArtistGroup>(artists.Count);
        foreach (var a in artists)
            if (!cooldownIds.Contains(a.ArtistId))
                eligible.Add(a);
        Shuffle(eligible, maxResults);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(a => a.Artist?.ArtistName ?? "")
            .ToList();
        var onCooldown = artists.Count - eligible.Count;

        _output.WriteDebug($"{inst}.missing",
            $"Artist cooldown filter: {onCooldown} on cooldown, {eligible.Count} eligible, {selected.Count} selected");

        var eligibleCount = eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(artists.Count, onCooldown, eligibleCount,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            _output.WriteWarning($"{inst}.missing", "No enabled indexers — search skipped",
                indexerNames is { Count: > 0 } ? $"Configured indexers: {string.Join(", ", indexerNames)}" : "No automatic-search indexers found");
            progress.WriteStats(artists.Count, onCooldown, eligibleCount, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} artists");
        progress.WriteStats(artists.Count, onCooldown, eligibleCount, selected.Count, false);
        progress.StartResults();

        foreach (var a in selected)
        {
            var title = a.Artist?.ArtistName ?? $"Artist {a.ArtistId}";

            progress.WriteItem(title);

            try { await client.TriggerArtistSearchAsync(a.ArtistId, ct); }
            catch (Exception ex)
            {
                _output.WriteWarning($"{inst}.missing",
                    $"Search trigger failed for {title}", ex.Message);
            }
        }

        var artistIds = selected.Select(s => s.ArtistId).ToArray();
        await _cooldown.MarkSearchedAsync(client.Instance, artistCategory, artistIds, ct);
        progress.WriteTrailer();
    }

    private readonly record struct ArtistGroup(int ArtistId, WantedAlbumArtistResource? Artist);

    private async Task RunMovieSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedMovieResource>>> getWanted, Func<int[], Task> triggerSearch,
        IReadOnlyList<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
        var inst = client.Instance.ToLowerInvariant();

        progress.SetPhase("Cleaning cooldown entries");
        await _cooldown.CleanExpiredAsync(client.Instance, category, cooldown, ct);

        progress.SetPhase("Fetching wanted movies");
        var wanted = await getWanted();

        _output.WriteDebug($"{inst}.missing", $"Fetched {wanted.Count} wanted movies");

        if (wanted.Count == 0)
        {
            progress.WriteStats(0, 0, 0, 0, true);
            return;
        }

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, category, ct);

        var eligible = new List<WantedMovieResource>(wanted.Count);
        foreach (var m in wanted)
            if (!cooldownIds.Contains(m.Id))
                eligible.Add(m);
        Shuffle(eligible, maxResults);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(m => m.Title ?? "")
            .ToList();
        var onCooldown = wanted.Count - eligible.Count;

        _output.WriteDebug($"{inst}.missing",
            $"Cooldown filter: {onCooldown} on cooldown, {eligible.Count} eligible, {selected.Count} selected");

        var eligibleCount = eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(wanted.Count, onCooldown, eligibleCount,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            _output.WriteWarning($"{inst}.missing", "No enabled indexers — search skipped",
                indexerNames is { Count: > 0 } ? $"Configured indexers: {string.Join(", ", indexerNames)}" : "No automatic-search indexers found");
            progress.WriteStats(wanted.Count, onCooldown, eligibleCount, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} items");
        progress.WriteStats(wanted.Count, onCooldown, eligibleCount, selected.Count, false);
        progress.StartResults();

        foreach (var m in selected)
        {
            var title = m.Year > 0
                ? $"{m.Title ?? $"Movie {m.Id}"} ({m.Year})"
                : m.Title ?? $"Movie {m.Id}";

            progress.WriteItem(title);
        }

        var ids = selected.Select(s => s.Id).ToArray();
        try { await triggerSearch(ids); }
        catch (Exception ex)
        {
            var titles = selected.Select(m =>
                m.Year > 0
                    ? $"{m.Title ?? $"Movie {m.Id}"} ({m.Year})"
                    : m.Title ?? $"Movie {m.Id}");
            _output.WriteWarning($"{inst}.missing",
                $"Search trigger failed for {string.Join(", ", titles)}", ex.Message);
        }

        await _cooldown.MarkSearchedAsync(client.Instance, category, ids, ct);
        progress.WriteTrailer();
    }

    private static async Task<bool> HasMatchingIndexersAsync(IArrClient client, IReadOnlyList<string>? indexerNames, CancellationToken ct)
    {
        if (indexerNames is null || indexerNames.Count == 0)
            return await client.HasAnyEnabledIndexerAsync(ct);

        var configuredNames = new HashSet<string>(indexerNames, StringComparer.OrdinalIgnoreCase);
        var indexers = await client.GetIndexersAsync(ct);
        return indexers.Any(i =>
            i.EnableAutomaticSearch &&
            i.Name is not null &&
            configuredNames.Contains(i.Name));
    }

    private static void Shuffle<T>(List<T> list, int count)
    {
        var rng = Random.Shared;
        var n = Math.Min(count, list.Count);
        for (int i = 0; i < n; i++)
        {
            var j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
