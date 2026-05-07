using ArrWarden.Configuration;

namespace ArrWarden.Services;

public class OutputService
{
    private const int BannerWidth = 54;

    public static void WriteBanner(WardenOptions opts)
    {
        var bar = new string('=', BannerWidth);

        Console.WriteLine(bar);
        Console.WriteLine(PadCenter("ArrWarden", BannerWidth));
        Console.WriteLine(bar);

        if (opts.HasSonarr)
        {
            Console.WriteLine($"  Sonarr URL          {opts.SonarrUrl}");
            if (!string.IsNullOrWhiteSpace(opts.SonarrQueueCleanupCron))
                Console.WriteLine($"    Queue Cleanup     {opts.SonarrQueueCleanupCron}");
            if (!string.IsNullOrWhiteSpace(opts.SonarrMissingSearchCron))
                Console.WriteLine($"    Missing Search    {opts.SonarrMissingSearchCron}");
            if (!string.IsNullOrWhiteSpace(opts.SonarrUpgradeSearchCron))
                Console.WriteLine($"    Upgrade Search    {opts.SonarrUpgradeSearchCron}");
        }

        if (opts.HasRadarr)
        {
            Console.WriteLine($"  Radarr URL          {opts.RadarrUrl}");
            if (!string.IsNullOrWhiteSpace(opts.RadarrQueueCleanupCron))
                Console.WriteLine($"    Queue Cleanup     {opts.RadarrQueueCleanupCron}");
            if (!string.IsNullOrWhiteSpace(opts.RadarrMissingSearchCron))
                Console.WriteLine($"    Missing Search    {opts.RadarrMissingSearchCron}");
            if (!string.IsNullOrWhiteSpace(opts.RadarrUpgradeSearchCron))
                Console.WriteLine($"    Upgrade Search    {opts.RadarrUpgradeSearchCron}");
        }

        Console.WriteLine($"  Dry Run             {opts.IsDryRun.ToString().ToLowerInvariant()}");
        Console.WriteLine(bar);
        Console.WriteLine();
    }

    public void WriteQueueResult(DateTime timestamp, string instance, int totalQueue, int blocked, int matched,
        IReadOnlyList<(string Title, string Rule)> items, bool isDryRun)
    {
        var ts = timestamp.ToString("HH:mm:ss");
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
            var ts = DateTime.Now.ToString("HH:mm:ss");
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
        return new string(' ', left) + text;
    }
}
