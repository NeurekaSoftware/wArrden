using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Services;

public class QueueCleanupService
{
    private readonly IArrClient _client;
    private readonly string _arrType;
    private readonly bool _isDryRun;
    private readonly OutputService _output;
    private readonly List<QueueCleanupRule>? _rules;

    public QueueCleanupService(IArrClient client, string instanceType, bool isDryRun, OutputService output,
        List<QueueCleanupRule>? rules = null)
    {
        _client = client;
        _arrType = instanceType.ToLowerInvariant();
        _isDryRun = isDryRun;
        _output = output;
        _rules = rules;
    }

    public async Task<int> CleanAsync(CancellationToken ct)
    {
        var instanceKey = _client.Instance.ToLowerInvariant();
        var rules = _rules;

        var queue = await _client.GetQueueAsync(ct);
        _output.WriteDebug($"{instanceKey}.queue", $"Fetched {queue.Count} queue items");

        if (rules is null || rules.Count == 0)
        {
            _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, 0, 0,
                Array.Empty<(int, string, string, bool)>(), _isDryRun);
            return 0;
        }

        var blocked = queue.Where(q =>
            string.Equals(q.TrackedDownloadStatus, "warning", StringComparison.OrdinalIgnoreCase))
            .ToList();

        _output.WriteDebug($"{instanceKey}.queue", $"{blocked.Count} warning-status items out of {queue.Count} total");

        if (blocked.Count == 0)
        {
            _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, 0, 0,
                Array.Empty<(int, string, string, bool)>(), _isDryRun);
            return 0;
        }

        var matched = new List<(int Id, string Title, string Rule, bool Blocklist)>(blocked.Count);
        foreach (var item in blocked)
        {
            var match = MatchRule(item, rules, _arrType);
            if (match is null)
                continue;

            if (!_isDryRun)
            {
                try
                {
                    if (match.Value.Blocklist)
                        await _client.DeleteQueueItemAsync(item.Id, ct);
                    else
                        await _client.DeleteQueueItemWithoutBlocklistAsync(item.Id, ct);
                }
                catch (Exception ex)
                {
                    _output.WriteWarning($"{instanceKey}.queue",
                        $"Failed to remove queue item {item.Id} — {GetTitle(item)}", ex.Message);
                    continue;
                }
            }

            matched.Add((item.Id, GetTitle(item), match.Value.Label, match.Value.Blocklist));
        }

        _output.WriteDebug($"{instanceKey}.queue", $"Matched {matched.Count} items to cleanup rules");

        matched.Sort(CompareByTitle);

        _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, blocked.Count, matched.Count,
            matched, _isDryRun);
        return matched.Count;
    }

    internal static (string Label, bool Blocklist)? MatchRule(QueueResource item, List<QueueCleanupRule> rules, string arrType)
    {
        foreach (var rule in rules)
        {
            var patterns = QueueCleanupRuleMatchers.GetPatterns(rule.Match, arrType);
            if (patterns is { Length: > 0 })
            {
                if (!string.IsNullOrWhiteSpace(item.ErrorMessage))
                {
                    foreach (var pattern in patterns)
                        if (item.ErrorMessage.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                            return (rule.Match, rule.Blocklist);
                }

                if (item.StatusMessages is not null)
                {
                    foreach (var sm in item.StatusMessages)
                    {
                        if (sm.Messages is not null)
                        {
                            foreach (var msg in sm.Messages)
                            {
                                foreach (var pattern in patterns)
                                    if (msg.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                                        return (rule.Match, rule.Blocklist);
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private static int CompareByTitle((int Id, string Title, string Rule, bool Blocklist) a,
        (int Id, string Title, string Rule, bool Blocklist) b)
        => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);

    internal static string GetTitle(QueueResource item)
    {
        if (item.Episode is not null)
        {
            var ep = item.Episode;
            var epTitle = ep.Title ?? $"Episode {ep.Id}";
            if (ep.Series is not null)
                return $"{ep.Series.Title} ({ep.Series.Year}) - S{ep.SeasonNumber:D2}E{ep.EpisodeNumber:D2} - {epTitle}";
            return $"S{ep.SeasonNumber:D2}E{ep.EpisodeNumber:D2} - {epTitle}";
        }

        if (item.Movie is not null)
        {
            var movieTitle = item.Movie.Title ?? $"Movie {item.Movie.Id}";
            if (item.Movie.Year > 0)
                return $"{movieTitle} ({item.Movie.Year})";
            return movieTitle;
        }

        if (item.Artist is not null)
        {
            var artistName = item.Artist.ArtistName ?? $"Artist {item.Artist.Id}";
            if (item.Album?.Title is not null)
                return $"{artistName} - {item.Album.Title}";
            return artistName;
        }

        return item.Title ?? $"ID {item.Id}";
    }
}
