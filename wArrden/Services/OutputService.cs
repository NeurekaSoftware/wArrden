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

    private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

    public TextWriter Out { get; set; } = Console.Out;
    public TextWriter Error { get; set; } = Console.Out;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
    public TimeZoneInfo TimeZone { get => _timeZone; set => _timeZone = value; }

    public static void WriteBanner(AppConfig config, WardenOptions opts, TimeZoneInfo timeZone, TextWriter? writer = null)
    {
        var w = writer ?? Console.Out;
        var bar = new string('━', BoxWidth);
        w.WriteLine($"┏{bar}┓");
        w.WriteLine($"┃{PadCenter("wArrden", BoxWidth)}┃");
        w.WriteLine($"┗{bar}┛");
        w.WriteLine();

        var tz = timeZone;
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var ts = FormatTimestamp(now);

        w.WriteLine($"[{ts} INFO] [system.startup]");

        var enabledInstances = config.Instances.Where(i => i.Enabled == true).ToList();
        var totalSections = enabledInstances.Count + 2;
        var sectionIndex = 0;

        WriteSection(w, opts, tz, now, config, enabledInstances, totalSections, ref sectionIndex, isRuntime: true);
        for (int i = 0; i < enabledInstances.Count; i++)
            WriteSection(w, opts, tz, now, config, enabledInstances, totalSections, ref sectionIndex, isRuntime: false, instanceIndex: i);
        WriteSection(w, opts, tz, now, config, enabledInstances, totalSections, ref sectionIndex, isRuntime: false, isQueueRules: true);

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
        }

        if (config.Errors.Count > 0)
        {
            w.WriteLine();
            w.WriteLine($"\x1b[31m[{ts} ERROR] [warden.config]\x1b[0m");
            for (int i = 0; i < config.Errors.Count; i++)
            {
                var isLastError = i == config.Errors.Count - 1;
                var prefix = isLastError ? " └─" : " ├─";
                w.WriteLine($"{prefix} {config.Errors[i]}");
            }
        }

        w.WriteLine();
        w.WriteLine($"[{ts} INFO] [system.ready] wArrden initialized");
        w.WriteLine();
    }

    private static void WriteSection(TextWriter w, WardenOptions opts, TimeZoneInfo tz, DateTime now, AppConfig config,
        List<InstanceConfig> enabledInstances, int totalSections, ref int sectionIndex,
        bool isRuntime = false, int instanceIndex = -1, bool isQueueRules = false)
    {
        var isLast = sectionIndex == totalSections - 1;
        var rootPrefix = isLast ? " └─" : " ├─";
        var childPrefix = isLast ? "    " : " │  ";

        if (isRuntime)
            WriteRuntimeSection(w, rootPrefix, childPrefix, opts, tz, now);
        else if (isQueueRules)
            WriteQueueCleanupRulesSection(w, rootPrefix, childPrefix, config);
        else
            WriteInstanceSection(w, rootPrefix, childPrefix, enabledInstances[instanceIndex]);

        if (!isLast)
            w.WriteLine(" │");

        sectionIndex++;
    }

    private static void WriteInstanceSection(TextWriter w, string rootPrefix, string childPrefix, InstanceConfig inst)
    {
        w.WriteLine($"{rootPrefix} {inst.Name} ({inst.InstanceKey})");

        var children = new List<string>();
        children.Add($"URL".PadRight(LabelPad) + inst.Url);

        if (inst.QueueCleanup is not null)
        {
            if (inst.QueueCleanup.Enabled == true)
                children.Add($"Queue Cleanup".PadRight(LabelPad) + inst.QueueCleanup.Cron!);
            else
                children.Add($"Queue Cleanup".PadRight(LabelPad) + "(disabled)");
        }
        if (inst.MissingSearch is not null)
        {
            if (inst.MissingSearch.Enabled == true)
                children.Add($"Missing Search".PadRight(LabelPad) + inst.MissingSearch.Cron! + SearchTypeLabel(inst, inst.MissingSearch));
            else
                children.Add($"Missing Search".PadRight(LabelPad) + "(disabled)");
        }
        if (inst.UpgradeSearch is not null)
        {
            if (inst.UpgradeSearch.Enabled == true)
                children.Add($"Upgrade Search".PadRight(LabelPad) + inst.UpgradeSearch.Cron! + SearchTypeLabel(inst, inst.UpgradeSearch));
            else
                children.Add($"Upgrade Search".PadRight(LabelPad) + "(disabled)");
        }

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

        var version = opts.AppVersion ?? "dev";
        var dryRun = opts.IsDryRun.ToString().ToLowerInvariant();

        w.WriteLine($"{childPrefix} ├─ {"Version".PadRight(LabelPad)}{version}");
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

    private static string FormatTimestamp(DateTime dt) => dt.ToString("HH:mm:ss");

    private DateTime GetNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

    public virtual void WriteDebug(string context, string message)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        WriteLogLine("DEBUG", context, message, null);
    }

    public virtual void WriteDebug(string context, string message, string detail)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        WriteLogLine("DEBUG", context, message, detail);
    }

    public virtual void WriteWarning(string context, string message)
    {
        if (!ShouldLog(LogLevel.Warning)) return;
        WriteLogLine("WARN", context, message, null);
    }

    public virtual void WriteWarning(string context, string message, string detail)
    {
        if (!ShouldLog(LogLevel.Warning)) return;
        WriteLogLine("WARN", context, message, detail);
    }

    public virtual void WriteError(string context, string message)
    {
        if (!ShouldLog(LogLevel.Error)) return;
        WriteLogLine("ERROR", context, message, null);
    }

    public virtual void WriteError(string context, string message, Exception ex)
    {
        if (!ShouldLog(LogLevel.Error)) return;
        WriteLogLine("ERROR", context, message, $"{ex.GetType().Name}: {ex.Message}");
    }

    private bool ShouldLog(LogLevel level) => level >= MinimumLevel;

    private void WriteLogLine(string level, string context, string message, string? detail)
    {
        var color = AnsiColorForLevel(level);
        var ts = FormatTimestamp(GetNow());

        Out.WriteLine($"{color}[{ts} {level}] [{context}]\x1b[0m");

        if (detail is null)
        {
            Out.WriteLine($" └─ {message}");
        }
        else
        {
            Out.WriteLine($" ├─ {message}");
            Out.WriteLine($" └─ {detail}");
        }

        Out.WriteLine();
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

    public void WriteQueueResult(string instance, int totalQueue, int blocked, int matched,
        IReadOnlyList<(int Id, string Title, string Rule, bool Blocklist)> items, bool isDryRun)
    {
        if (!ShouldLog(LogLevel.Info)) return;

        var ts = FormatTimestamp(GetNow());
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

    public void WriteClearCooldownsResult(string label, string category,
        IReadOnlyList<(string Instance, int Count)> counts)
    {
        if (!ShouldLog(LogLevel.Info)) return;

        var ts = FormatTimestamp(GetNow());
        Out.WriteLine($"[{ts} INFO] [{label}]");

        var typeLabel = category == "Missing" ? "Missing" : "Upgrade";
        Out.WriteLine($" ├─ Type:       {typeLabel}");

        if (counts.Count == 1)
        {
            var count = counts[0].Count;
            Out.WriteLine($" └─ Cleared:    {count} entr{(count == 1 ? "y" : "ies")}");
        }
        else
        {
            foreach (var (instance, count) in counts)
                Out.WriteLine($" ├─ {instance}:     {count} entr{(count == 1 ? "y" : "ies")}");

            var total = counts.Sum(x => x.Count);
            Out.WriteLine($" └─ Cleared:    {total} entr{(total == 1 ? "y" : "ies")}");
        }

        Out.WriteLine();
    }

    public SearchOutputWriter CreateSearchWriter(string instance, string job, int maxResults)
    {
        return new SearchOutputWriter(instance, job, maxResults, _timeZone, Out, ShouldLog(LogLevel.Info));
    }

    public virtual async Task RunSearchWithOutput(string instance, string job, int maxResults,
        Func<SearchOutputWriter, Task> searchLogic)
    {
        var output = new SearchOutputWriter(instance, job, maxResults, _timeZone, Out, ShouldLog(LogLevel.Info));
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
        private readonly TimeZoneInfo _timeZone;

        internal SearchOutputWriter(string instance, string job, int maxResults, TimeZoneInfo timeZone, TextWriter writer, bool shouldLog = true)
        {
            _instance = instance;
            _job = job;
            _maxResults = maxResults;
            _timeZone = timeZone;
            _writer = writer;
            _shouldLog = shouldLog;
        }

        public virtual void WriteHeader()
        {
            if (!_shouldLog) return;
            var ts = FormatTimestamp(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone));
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
