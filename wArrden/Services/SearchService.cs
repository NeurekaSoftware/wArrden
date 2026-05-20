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

    public virtual async Task SearchMissingEpisodesAsync(IArrClient client, int maxResults, TimeSpan cooldown, string searchType, bool isDryRun, List<string>? indexerNames, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Missing Search", maxResults,
            async progress =>
            {
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
            });
    }

    public virtual async Task SearchUpgradeEpisodesAsync(IArrClient client, int maxResults, TimeSpan cooldown, string searchType, bool isDryRun, List<string>? indexerNames, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Upgrade Search", maxResults,
            async progress =>
            {
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
            });
    }

    public virtual async Task SearchMissingMoviesAsync(IArrClient client, int maxResults, TimeSpan cooldown, bool isDryRun, List<string>? indexerNames, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Missing Search", maxResults,
            async progress =>
            {
                await RunMovieSearch(client, "Missing", cooldown, maxResults, isDryRun,
                    () => client.GetWantedMissingMoviesAsync(ct), async ids => await client.TriggerMoviesSearchAsync(ids, ct), indexerNames, progress, ct);
            });
    }

    public virtual async Task SearchUpgradeMoviesAsync(IArrClient client, int maxResults, TimeSpan cooldown, bool isDryRun, List<string>? indexerNames, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Upgrade Search", maxResults,
            async progress =>
            {
                await RunMovieSearch(client, "Upgrade", cooldown, maxResults, isDryRun,
                    () => client.GetWantedCutoffMoviesAsync(ct), async ids => await client.TriggerMoviesSearchAsync(ids, ct), indexerNames, progress, ct);
            });
    }

    private async Task RunEpisodeSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedEpisodeResource>>> getWanted, Func<int[], Task> triggerSearch,
        List<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
        progress.SetPhase("Cleaning cooldown entries");
        await _cooldown.CleanExpiredAsync(client.Instance, category, cooldown, ct);

        progress.SetPhase("Fetching wanted episodes");
        var wanted = await getWanted();

        if (wanted.Count == 0)
        {
            progress.WriteStats(0, 0, 0, 0, true);
            return;
        }

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, category, ct);

        var eligible = wanted.Where(e => !cooldownIds.Contains(e.Id)).ToList();
        Shuffle(eligible);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(e => e.Series?.Title ?? "")
            .ThenBy(e => e.SeasonNumber)
            .ThenBy(e => e.EpisodeNumber)
            .ToList();
        var onCooldown = wanted.Count - eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(wanted.Count, onCooldown, eligible.Count,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            progress.WriteStats(wanted.Count, onCooldown, eligible.Count, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} items");
        progress.WriteStats(wanted.Count, onCooldown, eligible.Count, selected.Count, false);
        progress.StartResults();

        foreach (var ep in selected)
        {
            var title = ep.Title ?? $"Episode {ep.Id}";
            if (ep.Series is not null)
                title = $"{ep.Series.Title} ({ep.Series.Year}) - S{ep.SeasonNumber:D2}E{ep.EpisodeNumber:D2} - {title}";

            progress.WriteItem(title);

            try { await triggerSearch(new[] { ep.Id }); }
            catch { }
        }

        await _cooldown.MarkSearchedAsync(client.Instance, category, selected.Select(e => e.Id).ToArray(), ct);
        progress.WriteTrailer();
    }

    private async Task RunSeasonSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedEpisodeResource>>> getWanted,
        List<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
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
                // Encode (seriesId, seasonNumber) into a single int via positional notation.
                // Multiplier 1000 > max season number (~50), ensuring injectivity:
                //   seriesId = key / 1000, seasonNumber = key % 1000
                g.Key.SeriesId * 1000 + g.Key.SeasonNumber
            ))
            .ToList();

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, seasonCategory, ct);

        var eligible = seasons.Where(s => !cooldownIds.Contains(s.SeasonKey)).ToList();
        Shuffle(eligible);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(s => s.Series?.Title ?? "")
            .ThenBy(s => s.SeasonNumber)
            .ToList();
        var onCooldown = seasons.Count - eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(seasons.Count, onCooldown, eligible.Count,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            progress.WriteStats(seasons.Count, onCooldown, eligible.Count, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} seasons");
        progress.WriteStats(seasons.Count, onCooldown, eligible.Count, selected.Count, false);
        progress.StartResults();

        foreach (var s in selected)
        {
            var title = s.Series?.Title ?? $"Series {s.SeriesId}";
            if (s.Series is not null && s.Series.Year > 0)
                title = $"{title} ({s.Series.Year})";
            title = $"{title} - Season {s.SeasonNumber}";

            progress.WriteItem(title);

            try { await client.TriggerSeasonSearchAsync(s.SeriesId, s.SeasonNumber, ct); }
            catch { }
        }

        await _cooldown.MarkSearchedAsync(client.Instance, seasonCategory,
            selected.Select(s => s.SeasonKey).ToArray(), ct);
        progress.WriteTrailer();
    }

    private sealed record SeasonGroup(int SeriesId, int SeasonNumber, WantedEpisodeSeriesResource? Series, int SeasonKey);

    private async Task RunMovieSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults, bool isDryRun,
        Func<Task<IReadOnlyList<WantedMovieResource>>> getWanted, Func<int[], Task> triggerSearch,
        List<string>? indexerNames, OutputService.SearchOutputWriter progress, CancellationToken ct)
    {
        progress.SetPhase("Cleaning cooldown entries");
        await _cooldown.CleanExpiredAsync(client.Instance, category, cooldown, ct);

        progress.SetPhase("Fetching wanted movies");
        var wanted = await getWanted();

        if (wanted.Count == 0)
        {
            progress.WriteStats(0, 0, 0, 0, true);
            return;
        }

        progress.SetPhase("Applying cooldown filters");
        var cooldownIds = await _cooldown.GetCooldownIdsAsync(client.Instance, category, ct);

        var eligible = wanted.Where(m => !cooldownIds.Contains(m.Id)).ToList();
        Shuffle(eligible);
        var selected = eligible
            .Take(maxResults)
            .OrderBy(m => m.Title ?? "")
            .ToList();
        var onCooldown = wanted.Count - eligible.Count;

        if (selected.Count == 0 || isDryRun)
        {
            progress.WriteStats(wanted.Count, onCooldown, eligible.Count,
                isDryRun ? 0 : selected.Count, true);
            return;
        }

        progress.SetPhase("Checking indexer availability");
        if (!await HasMatchingIndexersAsync(client, indexerNames, ct))
        {
            progress.WriteStats(wanted.Count, onCooldown, eligible.Count, 0, true, "No enabled indexers available");
            return;
        }

        progress.SetPhase($"Searching {selected.Count} items");
        progress.WriteStats(wanted.Count, onCooldown, eligible.Count, selected.Count, false);
        progress.StartResults();

        foreach (var m in selected)
        {
            var title = m.Title ?? $"Movie {m.Id}";
            if (m.Year > 0)
                title = $"{title} ({m.Year})";

            progress.WriteItem(title);

            try { await triggerSearch(new[] { m.Id }); }
            catch { }
        }

        await _cooldown.MarkSearchedAsync(client.Instance, category, selected.Select(m => m.Id).ToArray(), ct);
        progress.WriteTrailer();
    }

    private static async Task<bool> HasMatchingIndexersAsync(IArrClient client, List<string>? indexerNames, CancellationToken ct)
    {
        if (indexerNames is null || indexerNames.Count == 0)
            return await client.HasAnyEnabledIndexerAsync(ct);

        var indexers = await client.GetIndexersAsync(ct);
        return indexers.Any(i =>
            i.EnableAutomaticSearch &&
            i.Name is not null &&
            indexerNames.Contains(i.Name, StringComparer.OrdinalIgnoreCase));
    }

    private static void Shuffle<T>(List<T> list)
    {
        var rng = Random.Shared;
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
