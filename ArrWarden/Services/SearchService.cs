using ArrWarden.Clients;
using ArrWarden.Configuration;
using ArrWarden.Clients.Models;

namespace ArrWarden.Services;

public class SearchService
{
    private readonly ICooldownService _cooldown;
    private readonly WardenOptions _options;
    private readonly OutputService _output;

    public SearchService(ICooldownService cooldown, WardenOptions options, OutputService output)
    {
        _cooldown = cooldown;
        _options = options;
        _output = output;
    }

    public async Task SearchMissingEpisodesAsync(IArrClient client, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Missing Search", _options.SonarrMissingMaxResults,
            async progress =>
            {
                await RunEpisodeSearch(client, "Missing", _options.SonarrMissingCooldown, _options.SonarrMissingMaxResults,
                    () => client.GetWantedMissingEpisodesAsync(ct), async ids => await client.TriggerEpisodeSearchAsync(ids, ct), progress, ct);
            });
    }

    public async Task SearchUpgradeEpisodesAsync(IArrClient client, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Upgrade Search", _options.SonarrUpgradeMaxResults,
            async progress =>
            {
                await RunEpisodeSearch(client, "Upgrade", _options.SonarrUpgradeCooldown, _options.SonarrUpgradeMaxResults,
                    () => client.GetWantedCutoffEpisodesAsync(ct), async ids => await client.TriggerEpisodeSearchAsync(ids, ct), progress, ct);
            });
    }

    public async Task SearchMissingMoviesAsync(IArrClient client, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Missing Search", _options.RadarrMissingMaxResults,
            async progress =>
            {
                await RunMovieSearch(client, "Missing", _options.RadarrMissingCooldown, _options.RadarrMissingMaxResults,
                    () => client.GetWantedMissingMoviesAsync(ct), async ids => await client.TriggerMoviesSearchAsync(ids, ct), progress, ct);
            });
    }

    public async Task SearchUpgradeMoviesAsync(IArrClient client, CancellationToken ct)
    {
        await _output.RunSearchWithOutput(client.Instance, "Upgrade Search", _options.RadarrUpgradeMaxResults,
            async progress =>
            {
                await RunMovieSearch(client, "Upgrade", _options.RadarrUpgradeCooldown, _options.RadarrUpgradeMaxResults,
                    () => client.GetWantedCutoffMoviesAsync(ct), async ids => await client.TriggerMoviesSearchAsync(ids, ct), progress, ct);
            });
    }

    private async Task RunEpisodeSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults,
        Func<Task<IReadOnlyList<WantedEpisodeResource>>> getWanted, Func<int[], Task> triggerSearch,
        OutputService.SearchOutputWriter progress, CancellationToken ct)
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
        var selected = eligible.Take(maxResults).ToList();
        var onCooldown = wanted.Count - eligible.Count;

        if (selected.Count == 0 || _options.IsDryRun)
        {
            progress.WriteStats(wanted.Count, onCooldown, eligible.Count,
                _options.IsDryRun ? 0 : selected.Count, true);
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

    private async Task RunMovieSearch(IArrClient client, string category, TimeSpan cooldown, int maxResults,
        Func<Task<IReadOnlyList<WantedMovieResource>>> getWanted, Func<int[], Task> triggerSearch,
        OutputService.SearchOutputWriter progress, CancellationToken ct)
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
        var selected = eligible.Take(maxResults).ToList();
        var onCooldown = wanted.Count - eligible.Count;

        if (selected.Count == 0 || _options.IsDryRun)
        {
            progress.WriteStats(wanted.Count, onCooldown, eligible.Count,
                _options.IsDryRun ? 0 : selected.Count, true);
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
