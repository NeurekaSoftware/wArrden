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
        var rules = GetEnabledRules();
        if (rules.Count == 0)
        {
            _output.WriteQueueResult(DateTime.Now, _client.Instance, "Queue Cleanup", 0, 0,
                Array.Empty<(string, string)>(), _options.IsDryRun);
            return 0;
        }

        var queue = await _client.GetQueueAsync(ct);
        var blocked = queue.Where(q =>
            string.Equals(q.TrackedDownloadStatus, "warning", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (blocked.Count == 0)
        {
            _output.WriteQueueResult(DateTime.Now, _client.Instance, "Queue Cleanup", 0, 0,
                Array.Empty<(string, string)>(), _options.IsDryRun);
            return 0;
        }

        var matched = new List<(int Id, string Title, string Rule)>();
        foreach (var item in blocked)
        {
            var messages = CollectMessages(item);
            var rule = MatchRule(messages, rules);
            if (rule is null)
                continue;

            if (!_options.IsDryRun)
                await _client.DeleteQueueItemAsync(item.Id, ct);

            matched.Add((item.Id, GetTitle(item), rule));
        }

        _output.WriteQueueResult(DateTime.Now, _client.Instance, "Queue Cleanup", blocked.Count, matched.Count,
            matched.Select(m => (m.Title, m.Rule)).ToList(), _options.IsDryRun);
        return matched.Count;
    }

    private Dictionary<string, string> GetEnabledRules()
    {
        var rules = _prefix == "SONARR" ? SonarrRules : RadarrRules;
        var blacklist = _prefix == "SONARR" ? _options.SonarrBlacklistRules : _options.RadarrBlacklistRules;

        var enabled = new Dictionary<string, string>();
        foreach (var kvp in rules)
        {
            var envKey = $"{_prefix}_BLACKLIST_{kvp.Key}";
            if (blacklist.TryGetValue(envKey, out var val) && val)
                enabled[kvp.Key] = kvp.Value;
        }
        return enabled;
    }

    private static string CollectMessages(QueueResource item)
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

    private static string? MatchRule(string messages, Dictionary<string, string> rules)
    {
        foreach (var (key, keyword) in rules)
        {
            if (messages.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return key;
        }
        return null;
    }

    private static string GetTitle(QueueResource item)
    {
        return item.Title
            ?? item.Episode?.Title
            ?? item.Movie?.Title
            ?? $"ID {item.Id}";
    }

    private static readonly Dictionary<string, string> SonarrRules = new()
    {
        ["NOT_A_CUSTOM_FORMAT_UPGRADE"] = "Not a Custom Format upgrade",
        ["NOT_AN_UPGRADE"] = "Not an upgrade for existing episode",
        ["NOT_A_REVISION_UPGRADE"] = "Not a quality revision upgrade",
        ["SAMPLE"] = "Sample",
        ["UNABLE_TO_PARSE"] = "Unable to parse",
        ["NO_FILES_ELIGIBLE"] = "No files found are eligible",
        ["SERIES_MISMATCH"] = "Series title mismatch",
        ["FOUND_MULTIPLE_SERIES"] = "found multiple series",
        ["FULL_SEASON"] = "all episodes in seasons",
        ["NO_AUDIO_TRACKS"] = "No audio tracks detected",
        ["EPISODE_NOT_IN_RELEASE"] = "not found in the grabbed release",
        ["UNVERIFIED_SCENE_MAPPING"] = "mapping",
    };

    private static readonly Dictionary<string, string> RadarrRules = new()
    {
        ["NOT_A_CUSTOM_FORMAT_UPGRADE"] = "Not a Custom Format upgrade",
        ["NOT_AN_UPGRADE"] = "Not an upgrade for existing movie",
        ["NOT_A_REVISION_UPGRADE"] = "Not a quality revision upgrade",
        ["SAMPLE"] = "Sample",
        ["UNABLE_TO_PARSE"] = "Unable to parse",
        ["NO_FILES_ELIGIBLE"] = "No files found are eligible",
        ["MOVIE_MISMATCH"] = "Movie title mismatch",
        ["FOUND_MULTIPLE_MOVIES"] = "found multiple movies",
        ["NO_AUDIO_TRACKS"] = "No audio tracks detected",
        ["MOVIE_NOT_IN_RELEASE"] = "not found in the grabbed release",
    };
}
