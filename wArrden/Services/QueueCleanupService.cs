using wArrden.Clients;
using wArrden.Clients.Models;

namespace wArrden.Services;

public class QueueCleanupService
{
    private readonly IArrClient _client;
    private readonly string _instanceType;
    private readonly bool _isDryRun;
    private readonly OutputService _output;
    private readonly List<QueueCleanupRule>? _rules;

    public QueueCleanupService(IArrClient client, string instanceType, bool isDryRun, OutputService output,
        List<QueueCleanupRule>? rules = null)
    {
        _client = client;
        _instanceType = instanceType;
        _isDryRun = isDryRun;
        _output = output;
        _rules = rules;
    }

    public async Task<int> CleanAsync(CancellationToken ct)
    {
        var rules = _rules;

        var queue = await _client.GetQueueAsync(ct);
        _output.WriteDebug($"{_client.Instance.ToLowerInvariant()}.queue", $"Fetched {queue.Count} queue items");

        if (rules is null || rules.Count == 0)
        {
            _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, 0, 0,
                Array.Empty<(string, string, bool)>(), _isDryRun);
            return 0;
        }

        var blocked = queue.Where(q =>
            string.Equals(q.TrackedDownloadStatus, "warning", StringComparison.OrdinalIgnoreCase))
            .ToList();

        _output.WriteDebug($"{_client.Instance.ToLowerInvariant()}.queue", $"{blocked.Count} warning-status items out of {queue.Count} total");

        if (blocked.Count == 0)
        {
            _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, 0, 0,
                Array.Empty<(string, string, bool)>(), _isDryRun);
            return 0;
        }

        var matched = new List<(int Id, string Title, string Rule, bool Blocklist)>();
        foreach (var item in blocked)
        {
            var messages = CollectMessages(item);
            var match = MatchRule(messages, rules);
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
                    _output.WriteWarning($"{_client.Instance.ToLowerInvariant()}.queue",
                        $"Failed to remove queue item {item.Id} — {GetTitle(item)}", ex.Message);
                    continue;
                }
            }

            matched.Add((item.Id, GetTitle(item), match.Value.Label, match.Value.Blocklist));
        }

        _output.WriteDebug($"{_client.Instance.ToLowerInvariant()}.queue", $"Matched {matched.Count} items to cleanup rules");

        var sorted = matched.OrderBy(m => m.Title, StringComparer.OrdinalIgnoreCase).ToList();

        _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, blocked.Count, sorted.Count,
            sorted.Select(m => (m.Title, m.Rule, m.Blocklist)).ToList(), _isDryRun);
        return matched.Count;
    }

    internal static string CollectMessages(QueueResource item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.ErrorMessage))
            parts.Add(item.ErrorMessage);

        if (item.StatusMessages is not null)
        {
            foreach (var sm in item.StatusMessages)
            {
                if (sm.Messages is not null)
                    parts.AddRange(sm.Messages);
            }
        }

        return string.Join(" ", parts);
    }

    internal static (string Label, bool Blocklist)? MatchRule(string messages, List<QueueCleanupRule> rules)
    {
        foreach (var rule in rules)
        {
            if (messages.Contains(rule.Match, StringComparison.OrdinalIgnoreCase))
                return (rule.Match, rule.Blocklist);
        }
        return null;
    }

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
