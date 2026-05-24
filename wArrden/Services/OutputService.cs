using wArrden.Configuration;

namespace wArrden.Services;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public class OutputService
{
    private const int BoxWidth = 58;
    private const int LabelPad = 18;

    public TextWriter Out { get; set; } = Console.Out;
    public TextWriter Error { get; set; } = Console.Error;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    public static void WriteBanner(AppConfig config, WardenOptions opts, TextWriter? writer = null)
    {
        var w = writer ?? Console.Out;
        var bar = new string('━', BoxWidth);
        w.WriteLine($"┏{bar}┓");
        w.WriteLine($"┃{PadCenter("wArrden", BoxWidth)}┃");
        w.WriteLine($"┗{bar}┛");
        w.WriteLine();

        var tz = ResolveTimezone(opts.Timezone);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var ts = FormatTimestamp(now);

        w.WriteLine($"[{ts} INFO] [system.startup]");

        for (int i = 0; i < config.Instances.Count + 2; i++)
        {
            var isLast = i == config.Instances.Count + 1;
            var rootPrefix = isLast ? " └─" : " ├─";
            var childPrefix = isLast ? "    " : " │  ";

            if (i < config.Instances.Count)
                WriteInstanceSection(w, rootPrefix, childPrefix, config.Instances[i]);
            else if (i == config.Instances.Count)
                WriteRuntimeSection(w, rootPrefix, childPrefix, opts, tz, now);
            else
                WriteQueueCleanupRulesSection(w, rootPrefix, childPrefix, config);

            if (!isLast)
                w.WriteLine(" │");
        }

        if (config.Warnings.Count > 0)
        {
            w.WriteLine();
            w.WriteLine($"\x1b[33m[{ts} WARN] [warden.config]\x1b[0m");
            for (int i = 0; i < config.Warnings.Count; i++)
            {
                var isLastWarning = i == config.Warnings.Count - 1;
                var prefix = isLastWarning ? " └─" : " ├─";
                w.WriteLine($"{prefix} {config.Warnings[i]}");
            }
            w.WriteLine();
        }

        w.WriteLine();
        w.WriteLine($"[{ts} INFO] [system.ready] wArrden initialized");
        w.WriteLine();
    }

    private static void WriteInstanceSection(TextWriter w, string rootPrefix, string childPrefix, InstanceConfig inst)
    {
        w.WriteLine($"{rootPrefix} {inst.Name} ({inst.InstanceKey})");

        var children = new List<string>();
        children.Add($"URL".PadRight(LabelPad) + inst.Url);

        if (inst.QueueCleanup?.Enabled == true)
            children.Add($"Queue Cleanup".PadRight(LabelPad) + inst.QueueCleanup.Cron!);
        if (inst.MissingSearch?.Enabled == true)
            children.Add($"Missing Search".PadRight(LabelPad) + inst.MissingSearch.Cron! + SearchTypeLabel(inst, inst.MissingSearch));
        if (inst.UpgradeSearch?.Enabled == true)
            children.Add($"Upgrade Search".PadRight(LabelPad) + inst.UpgradeSearch.Cron! + SearchTypeLabel(inst, inst.UpgradeSearch));

        for (int i = 0; i < children.Count; i++)
        {
            var isLastChild = i == children.Count - 1;
            var prefix = isLastChild ? " └─" : " ├─";
            w.WriteLine($"{childPrefix}{prefix} {children[i]}");
        }
    }

    private static void WriteRuntimeSection(TextWriter w, string rootPrefix, string childPrefix, WardenOptions opts,
        TimeZoneInfo tz, DateTime now)
    {
        w.WriteLine($"{rootPrefix} Runtime");

        var isDst = tz.IsDaylightSavingTime(now);
        var tzDisplayName = isDst ? tz.DaylightName : tz.StandardName;
        var abbr = GetTimeZoneAbbreviation(tzDisplayName);

        var displayId = GetTimezoneDisplayId(tz, opts.Timezone);

        var offset = tz.GetUtcOffset(now);
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var offsetStr = $"{sign}{Math.Abs(offset.Hours):D2}:{Math.Abs(offset.Minutes):D2}";

        var localTime = now.ToString("yyyy-MM-dd HH:mm:ss");

        var dryRun = opts.IsDryRun.ToString().ToLowerInvariant();

        w.WriteLine($"{childPrefix} ├─ {"Timezone".PadRight(LabelPad)}{displayId} ({abbr})");
        w.WriteLine($"{childPrefix} ├─ {"Local Time".PadRight(LabelPad)}{localTime}");
        w.WriteLine($"{childPrefix} ├─ {"UTC Offset".PadRight(LabelPad)}{offsetStr}");
        w.WriteLine($"{childPrefix} └─ {"Dry Run".PadRight(LabelPad)}{dryRun}");
    }

    private static void WriteQueueCleanupRulesSection(TextWriter w, string rootPrefix, string childPrefix, AppConfig config)
    {
        var sonarrCount = config.QueueCleanupRules?.Sonarr?.Count ?? 0;
        var radarrCount = config.QueueCleanupRules?.Radarr?.Count ?? 0;
        var lidarrCount = config.QueueCleanupRules?.Lidarr?.Count ?? 0;
        var whisparrCount = config.QueueCleanupRules?.Whisparr?.Count ?? 0;

        w.WriteLine($"{rootPrefix} Queue Cleanup Rules");
        w.WriteLine($"{childPrefix} ├─ {"sonarr".PadRight(LabelPad)}{sonarrCount} matcher(s)");
        w.WriteLine($"{childPrefix} ├─ {"radarr".PadRight(LabelPad)}{radarrCount} matcher(s)");
        w.WriteLine($"{childPrefix} ├─ {"lidarr".PadRight(LabelPad)}{lidarrCount} matcher(s)");
        w.WriteLine($"{childPrefix} └─ {"whisparr".PadRight(LabelPad)}{whisparrCount} matcher(s)");
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
        var span = displayName.AsSpan();
        Span<char> buffer = stackalloc char[8];
        var pos = 0;
        foreach (var range in span.Split(' '))
        {
            if (pos >= buffer.Length) break;
            var word = span[range];
            if (!word.IsEmpty)
                buffer[pos++] = char.ToUpper(word[0]);
        }
        return new string(buffer[..pos]);
    }

    private static string FormatTimestamp(DateTime dt) => dt.ToString("MM/dd/yyyy hh:mm:ss tt");

    public virtual void WriteDebug(string context, string message)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        WriteLogLine(Out, "DEBUG", context, message, null);
    }

    public virtual void WriteDebug(string context, string message, string detail)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        WriteLogLine(Out, "DEBUG", context, message, detail);
    }

    public virtual void WriteWarning(string context, string message)
    {
        if (!ShouldLog(LogLevel.Warning)) return;
        WriteLogLine(Out, "WARN", context, message, null);
    }

    public virtual void WriteWarning(string context, string message, string detail)
    {
        if (!ShouldLog(LogLevel.Warning)) return;
        WriteLogLine(Out, "WARN", context, message, detail);
    }

    public virtual void WriteError(string context, string message)
    {
        if (!ShouldLog(LogLevel.Error)) return;
        WriteLogLine(Out, "ERROR", context, message, null);
    }

    public virtual void WriteError(string context, string message, Exception ex)
    {
        if (!ShouldLog(LogLevel.Error)) return;
        WriteLogLine(Out, "ERROR", context, message, $"{ex.GetType().Name}: {ex.Message}");
    }

    private bool ShouldLog(LogLevel level) => level >= MinimumLevel;

    private static void WriteLogLine(TextWriter writer, string level, string context, string message, string? detail)
    {
        var color = AnsiColorForLevel(level);
        var ts = FormatTimestamp(DateTime.Now);

        writer.WriteLine($"{color}[{ts} {level}] [{context}]\x1b[0m");

        if (detail is null)
        {
            writer.WriteLine($" └─ {message}");
        }
        else
        {
            writer.WriteLine($" ├─ {message}");
            writer.WriteLine($" └─ {detail}");
        }

        writer.WriteLine();
    }

    private static string AnsiColorForLevel(string level)
    {
        return level switch
        {
            "DEBUG" => "\x1b[90m",
            "WARN" => "\x1b[33m",
            "ERROR" => "\x1b[31m",
            _ => string.Empty
        };
    }

    public void WriteQueueResult(DateTime timestamp, string instance, int totalQueue, int blocked, int matched,
        IReadOnlyList<(int Id, string Title, string Rule, bool Blocklist)> items, bool isDryRun)
    {
        if (!ShouldLog(LogLevel.Info)) return;

        var ts = FormatTimestamp(timestamp);
        var label = InstanceJobLabel(instance, "Queue Cleanup");
        Out.WriteLine($"[{ts} INFO] [{label}]");

        if (matched == 0)
        {
            Out.WriteLine(" └─ Stats:");
            Out.WriteLine($"    • Total Queue:   {totalQueue}");
            Out.WriteLine("    • Result:        No warning queue items detected");
        }
        else
        {
            var removedCount = items.Count(i => !i.Blocklist);
            var blocklistedCount = items.Count(i => i.Blocklist);
            var resultText = (isDryRun, removedCount > 0, blocklistedCount > 0) switch
            {
                (true, true, true) => $"Would remove {removedCount}, Would blocklist {blocklistedCount}",
                (false, true, true) => $"Removed {removedCount}, Blocklisted {blocklistedCount}",
                (true, true, false) => $"Would remove {removedCount}",
                (false, true, false) => $"Removed {removedCount}",
                (true, false, true) => $"Would blocklist {blocklistedCount}",
                (false, false, true) => $"Blocklisted {blocklistedCount}",
                _ => ""
            };

            Out.WriteLine(" ├─ Stats:");
            Out.WriteLine($" │  • Total Queue:   {totalQueue}");
            Out.WriteLine($" │  • Warnings:      {blocked}");
            Out.WriteLine($" │  • Matched:       {matched}");
            Out.WriteLine($" │  • Result:        {resultText}");
            Out.WriteLine(" └─ Results:");
            foreach (var (_, title, rule, _) in items)
                Out.WriteLine($"    • {title}  {rule}");
            if (items.Count < matched)
                Out.WriteLine($"    +{matched - items.Count} more");
        }

        Out.WriteLine();
    }

    public SearchOutputWriter CreateSearchWriter(string instance, string job, int maxResults)
    {
        return new SearchOutputWriter(instance, job, maxResults, Out, ShouldLog(LogLevel.Info));
    }

    public virtual async Task RunSearchWithOutput(string instance, string job, int maxResults,
        Func<SearchOutputWriter, Task> searchLogic)
    {
        var output = new SearchOutputWriter(instance, job, maxResults, Out, ShouldLog(LogLevel.Info));
        output.WriteHeader();
        await searchLogic(output);
    }

    public class SearchOutputWriter
    {
        private readonly TextWriter _writer;
        private readonly string _instance;
        private readonly string _job;
        private readonly int _maxResults;
        private readonly bool _shouldLog;

        internal SearchOutputWriter(string instance, string job, int maxResults, TextWriter writer, bool shouldLog = true)
        {
            _instance = instance;
            _job = job;
            _maxResults = maxResults;
            _writer = writer;
            _shouldLog = shouldLog;
        }

        public virtual void WriteHeader()
        {
            if (!_shouldLog) return;
            var ts = FormatTimestamp(DateTime.Now);
            var label = InstanceJobLabel(_instance, _job);
            _writer.WriteLine($"[{ts} INFO] [{label}]");
        }

        public virtual void SetPhase(string phase)
        {
            if (!_shouldLog) return;
            _writer.WriteLine($" ├─ {phase}");
        }

        public virtual void WriteStats(int totalCount, int onCooldown, int eligible, int searched, bool isLast, string? resultOverride = null)
        {
            if (!_shouldLog) return;

            var prefix = isLast ? " └─" : " ├─";
            var childPrefix = isLast ? "   " : " │ ";

            _writer.WriteLine($"{prefix} Stats:");
            _writer.WriteLine($"{childPrefix} • Total Items:   {totalCount}");
            _writer.WriteLine($"{childPrefix} • On Cooldown:   {onCooldown}");
            _writer.WriteLine($"{childPrefix} • Eligible:      {eligible}");
            _writer.WriteLine($"{childPrefix} • Search Limit:  {_maxResults}");

            string result;
            if (resultOverride is not null)
                result = resultOverride;
            else if (totalCount == 0)
                result = "No wanted items found";
            else if (searched == 0)
                result = "No search performed";
            else
                result = $"Searched {searched}";

            _writer.WriteLine($"{childPrefix} • Result:        {result}");
        }

        public virtual void StartResults()
        {
            if (!_shouldLog) return;
            _writer.WriteLine(" └─ Results:");
        }

        public virtual void WriteItem(string title)
        {
            if (!_shouldLog) return;
            _writer.WriteLine($"    • {title}");
        }

        public virtual void WriteTrailer()
        {
            if (!_shouldLog) return;
            _writer.WriteLine();
        }
    }

    private static string SearchTypeLabel(InstanceConfig inst, JobConfig job)
    {
        if ((!inst.IsSonarr && !inst.IsWhisparr && !inst.IsLidarr) || string.IsNullOrWhiteSpace(job.SearchType))
            return string.Empty;

        return $"  ({job.SearchType.ToLowerInvariant()})";
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
