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

    public void WriteQueueResult(DateTime timestamp, string instance, string job, int blocked, int matched,
        IReadOnlyList<(string Title, string Rule)> items, bool isDryRun)
    {
        var ts = timestamp.ToString("HH:mm:ss");
        var label = InstanceLabel(instance);

        if (matched == 0)
        {
            Console.WriteLine($"{ts}  {label} {job}  =>  blocked: {blocked}  no rules activated");
        }
        else
        {
            var verb = isDryRun ? "would blocklist" : "blocklisted";
            var prefix = isDryRun ? "[DRY RUN] " : "";
            Console.WriteLine($"{ts}  {label} {job}  =>  blocked: {blocked}  matched: {matched}  {prefix}{verb}");

            foreach (var (title, rule) in items)
            {
                var truncated = Truncate(title, 48);
                Console.WriteLine($"  {truncated,-48}  {rule}");
            }

            if (items.Count < matched)
                Console.WriteLine($"  +{matched - items.Count} more");
        }
    }

    public async Task RunSearchWithProgress(string instance, string job, int maxResults,
        Func<SearchProgress, Task> searchLogic)
    {
        var progress = new SearchProgress
        {
            Instance = instance,
            Job = job,
            MaxResults = maxResults,
        };

        progress.SetPhaseAction = (phase) =>
        {
            var ts = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"{ts}  {InstanceLabel(instance)} {job}  =>  {phase}");
        };

        progress.ItemSearchedAction = (title) =>
        {
            Console.WriteLine($"  {Truncate(title, 60)}");
        };

        progress.CompleteAction = (totalWanted, onCooldown, searched) =>
        {
            var ts = DateTime.Now.ToString("HH:mm:ss");
            var label = InstanceLabel(instance);

            if (totalWanted == 0)
                Console.WriteLine($"{ts}  {label} {job}  =>  no wanted items found");
            else if (searched == 0)
                Console.WriteLine($"{ts}  {label} {job}  =>  total: {totalWanted}  on cooldown: {onCooldown}  eligible: {Math.Max(0, totalWanted - onCooldown)}  max: {maxResults}  nothing to search");
            else
                Console.WriteLine($"{ts}  {label} {job}  =>  total: {totalWanted}  on cooldown: {onCooldown}  eligible: {Math.Max(0, totalWanted - onCooldown)}  max: {maxResults}  searched: {searched}");
            Console.WriteLine();
        };

        await searchLogic(progress);
    }

    public class SearchProgress
    {
        public string Instance { get; set; } = "";
        public string Job { get; set; } = "";
        public int MaxResults { get; set; }

        internal Action<string>? SetPhaseAction { get; set; }
        internal Action<string>? ItemSearchedAction { get; set; }
        internal Action<int, int, int>? CompleteAction { get; set; }

        public void SetPhase(string phase) => SetPhaseAction?.Invoke(phase);
        public void ItemSearched(string title) => ItemSearchedAction?.Invoke(title);
        public void Complete(int totalWanted, int onCooldown, int searched) => CompleteAction?.Invoke(totalWanted, onCooldown, searched);
    }

    private static string InstanceLabel(string name)
    {
        return $"[{name.ToUpperInvariant()}]";
    }

    private static string PadCenter(string text, int width)
    {
        var padding = width - text.Length;
        if (padding <= 0) return text;
        var left = padding / 2;
        return new string(' ', left) + text;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? "";
        return value[..(maxLength - 3)] + "...";
    }
}
