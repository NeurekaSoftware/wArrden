using wArrden.Configuration;

namespace wArrden.Services;

public class OutputService
{
    private const int BoxWidth = 58;
    private const int LabelPad = 18;

    public static void WriteBanner(WardenOptions opts)
    {
        var bar = new string('━', BoxWidth);
        Console.WriteLine($"┏{bar}┓");
        Console.WriteLine($"┃{PadCenter("wArrden", BoxWidth)}┃");
        Console.WriteLine($"┗{bar}┛");
        Console.WriteLine();

        var tz = ResolveTimezone(opts.Timezone);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var ts = FormatTimestamp(now);

        Console.WriteLine($"[{ts} INF] [system.startup]");

        var sections = new List<string>();
        if (opts.HasRadarr) sections.Add("Radarr");
        if (opts.HasSonarr) sections.Add("Sonarr");
        sections.Add("Runtime");

        for (int i = 0; i < sections.Count; i++)
        {
            var isLast = i == sections.Count - 1;
            var rootPrefix = isLast ? " └─" : " ├─";
            var childPrefix = isLast ? "    " : " │  ";

            switch (sections[i])
            {
                case "Sonarr":
                    WriteInstanceSection(rootPrefix, childPrefix, "Sonarr", opts.SonarrUrl!,
                        opts.SonarrQueueCleanupCron, opts.SonarrMissingSearchCron, opts.SonarrUpgradeSearchCron);
                    break;
                case "Radarr":
                    WriteInstanceSection(rootPrefix, childPrefix, "Radarr", opts.RadarrUrl!,
                        opts.RadarrQueueCleanupCron, opts.RadarrMissingSearchCron, opts.RadarrUpgradeSearchCron);
                    break;
                case "Runtime":
                    WriteRuntimeSection(rootPrefix, childPrefix, opts, tz, now);
                    break;
            }

            if (!isLast)
                Console.WriteLine(" │");
        }

        Console.WriteLine();
        Console.WriteLine($"[{ts} INF] [system.ready] wArrden initialized");
        Console.WriteLine();
    }

    private static void WriteInstanceSection(string rootPrefix, string childPrefix, string name, string url,
        string? queueCron, string? missingCron, string? upgradeCron)
    {
        var children = new List<string>();
        children.Add($"URL".PadRight(LabelPad) + url);
        if (!string.IsNullOrWhiteSpace(queueCron))
            children.Add($"Queue Cleanup".PadRight(LabelPad) + queueCron);
        if (!string.IsNullOrWhiteSpace(missingCron))
            children.Add($"Missing Search".PadRight(LabelPad) + missingCron);
        if (!string.IsNullOrWhiteSpace(upgradeCron))
            children.Add($"Upgrade Search".PadRight(LabelPad) + upgradeCron);

        Console.WriteLine($"{rootPrefix} {name}");

        for (int i = 0; i < children.Count; i++)
        {
            var isLastChild = i == children.Count - 1;
            var prefix = isLastChild ? " └─" : " ├─";
            Console.WriteLine($"{childPrefix}{prefix} {children[i]}");
        }
    }

    private static void WriteRuntimeSection(string rootPrefix, string childPrefix, WardenOptions opts,
        TimeZoneInfo tz, DateTime now)
    {
        Console.WriteLine($"{rootPrefix} Runtime");

        var isDst = tz.IsDaylightSavingTime(now);
        var tzDisplayName = isDst ? tz.DaylightName : tz.StandardName;
        var abbr = GetTimeZoneAbbreviation(tzDisplayName);

        var displayId = GetTimezoneDisplayId(tz, opts.Timezone);

        var offset = tz.GetUtcOffset(now);
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var offsetStr = $"{sign}{Math.Abs(offset.Hours):D2}:{Math.Abs(offset.Minutes):D2}";

        var localTime = now.ToString("yyyy-MM-dd HH:mm:ss");

        var dryRun = opts.IsDryRun.ToString().ToLowerInvariant();

        Console.WriteLine($"{childPrefix} ├─ {"Timezone".PadRight(LabelPad)}{displayId} ({abbr})");
        Console.WriteLine($"{childPrefix} ├─ {"Local Time".PadRight(LabelPad)}{localTime}");
        Console.WriteLine($"{childPrefix} ├─ {"UTC Offset".PadRight(LabelPad)}{offsetStr}");
        Console.WriteLine($"{childPrefix} └─ {"Dry Run".PadRight(LabelPad)}{dryRun}");
    }

    private static TimeZoneInfo ResolveTimezone(string? tzId)
    {
        if (!string.IsNullOrWhiteSpace(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch { }
        }
        return TimeZoneInfo.Local;
    }

    private static string GetTimezoneDisplayId(TimeZoneInfo tz, string? configuredId)
    {
        if (!string.IsNullOrWhiteSpace(configuredId))
            return configuredId;

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out var ianaId))
            return ianaId;

        return tz.Id;
    }

    private static string GetTimeZoneAbbreviation(string displayName)
    {
        return new string(displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpper(w[0]))
            .ToArray());
    }

    private static string FormatTimestamp(DateTime dt) => dt.ToString("MM/dd/yyyy hh:mm:ss tt");

    public void WriteQueueResult(DateTime timestamp, string instance, int totalQueue, int blocked, int matched,
        IReadOnlyList<(string Title, string Rule)> items, bool isDryRun)
    {
        var ts = FormatTimestamp(timestamp);
        var label = InstanceJobLabel(instance, "Queue Cleanup");
        Console.WriteLine($"[{ts} INF] [{label}]");

        if (matched == 0)
        {
            Console.WriteLine(" └─ Stats:");
            Console.WriteLine($"    • Total Queue:   {totalQueue}");
            Console.WriteLine("    • Result:        No blocked queue items detected");
        }
        else
        {
            var verb = isDryRun ? "Would blocklist" : "Blocklisted";
            Console.WriteLine(" ├─ Stats:");
            Console.WriteLine($" │  • Total Queue:   {totalQueue}");
            Console.WriteLine($" │  • Blocked:       {blocked}");
            Console.WriteLine($" │  • Matched:       {matched}");
            Console.WriteLine($" │  • Result:        {verb} {matched}");
            Console.WriteLine(" └─ Results:");
            foreach (var (title, rule) in items)
                Console.WriteLine($"    • {title}  {rule}");
            if (items.Count < matched)
                Console.WriteLine($"    +{matched - items.Count} more");
        }

        Console.WriteLine();
    }

    public virtual async Task RunSearchWithOutput(string instance, string job, int maxResults,
        Func<SearchOutputWriter, Task> searchLogic)
    {
        var output = new SearchOutputWriter(instance, job, maxResults);
        output.WriteHeader();
        await searchLogic(output);
    }

    public class SearchOutputWriter
    {
        private readonly string _instance;
        private readonly string _job;
        private readonly int _maxResults;

        internal SearchOutputWriter(string instance, string job, int maxResults)
        {
            _instance = instance;
            _job = job;
            _maxResults = maxResults;
        }

        public virtual void WriteHeader()
        {
            var ts = FormatTimestamp(DateTime.Now);
            var label = InstanceJobLabel(_instance, _job);
            Console.WriteLine($"[{ts} INF] [{label}]");
        }

        public virtual void SetPhase(string phase)
        {
            Console.WriteLine($" ├─ {phase}");
        }

        public virtual void WriteStats(int totalCount, int onCooldown, int eligible, int searched, bool isLast)
        {
            var prefix = isLast ? " └─" : " ├─";
            var childPrefix = isLast ? "   " : " │ ";

            Console.WriteLine($"{prefix} Stats:");
            Console.WriteLine($"{childPrefix} • Total Items:   {totalCount}");
            Console.WriteLine($"{childPrefix} • On Cooldown:   {onCooldown}");
            Console.WriteLine($"{childPrefix} • Eligible:      {eligible}");
            Console.WriteLine($"{childPrefix} • Search Limit:  {_maxResults}");

            string result;
            if (totalCount == 0)
                result = "No wanted items found";
            else if (searched == 0)
                result = "No search performed";
            else
                result = $"Searched {searched}";

            Console.WriteLine($"{childPrefix} • Result:        {result}");
        }

        public virtual void StartResults()
        {
            Console.WriteLine(" └─ Results:");
        }

        public virtual void WriteItem(string title)
        {
            Console.WriteLine($"    • {title}");
        }

        public virtual void WriteTrailer()
        {
            Console.WriteLine();
        }
    }

    private static string InstanceJobLabel(string instance, string job)
    {
        var jobKey = job.ToLowerInvariant() switch
        {
            "missing search" => "missing",
            "upgrade search" => "upgrade",
            "queue cleanup" => "queue",
            _ => job.ToLowerInvariant()
        };
        return $"{instance.ToLowerInvariant()}.{jobKey}";
    }

    private static string PadCenter(string text, int width)
    {
        var padding = width - text.Length;
        if (padding <= 0) return text;
        var left = padding / 2;
        var right = width - left - text.Length;
        return new string(' ', left) + text + new string(' ', right);
    }
}
