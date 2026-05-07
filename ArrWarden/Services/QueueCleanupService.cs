using ArrWarden.Clients;
using ArrWarden.Clients.Models;
using ArrWarden.Configuration;

namespace ArrWarden.Services;

public class QueueCleanupService
{
    private readonly IArrClient _client;
    private readonly WardenOptions _options;
    private readonly string _prefix;
    private readonly OutputService _output;

    public QueueCleanupService(IArrClient client, WardenOptions options, string envPrefix, OutputService output)
    {
        _client = client;
        _options = options;
        _prefix = envPrefix;
        _output = output;
    }

    public async Task<int> CleanAsync(CancellationToken ct)
    {
        var rules = _prefix == "SONARR" ? QueueCleanupRules.Sonarr : QueueCleanupRules.Radarr;

        var queue = await _client.GetQueueAsync(ct);
        var blocked = queue.Where(q =>
            string.Equals(q.TrackedDownloadStatus, "warning", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (blocked.Count == 0)
        {
            _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, 0, 0,
                Array.Empty<(string, string)>(), _options.IsDryRun);
            return 0;
        }

        var matched = new List<(int Id, string Title, string Rule)>();
        foreach (var item in blocked)
        {
            var messages = CollectMessages(item);
            var match = MatchRule(messages, rules);
            if (match is null)
                continue;

            if (!_options.IsDryRun)
            {
                if (match.Value.Blocklist)
                    await _client.DeleteQueueItemAsync(item.Id, ct);
                else
                    await _client.DeleteQueueItemWithoutBlocklistAsync(item.Id, ct);
            }

            matched.Add((item.Id, GetTitle(item), match.Value.Key));
        }

        _output.WriteQueueResult(DateTime.Now, _client.Instance, queue.Count, blocked.Count, matched.Count,
            matched.Select(m => (m.Title, m.Rule)).ToList(), _options.IsDryRun);
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

    internal static (string Key, bool Blocklist)? MatchRule(string messages, Dictionary<string, (string Match, bool Blocklist)> rules)
    {
        foreach (var (key, (match, blocklist)) in rules)
        {
            if (messages.Contains(match, StringComparison.OrdinalIgnoreCase))
                return (key, blocklist);
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

        return item.Title ?? $"ID {item.Id}";
    }
}
