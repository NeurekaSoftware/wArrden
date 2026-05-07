using ArrWarden.Configuration;
using Spectre.Console;

namespace ArrWarden.Services;

public class OutputService
{
    private static readonly bool Interactive = false;

    public static string InstanceMarkup(string name) => name switch
    {
        "Sonarr" => "[aqua]Sonarr[/]",
        "Radarr" => "[yellow]Radarr[/]",
        _ => $"[white]{name}[/]"
    };

    public static Color InstanceColor(string name) => name switch
    {
        "Sonarr" => Color.Aqua,
        "Radarr" => Color.Yellow,
        _ => Color.White
    };

    public static void WriteBanner(WardenOptions opts)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        if (opts.HasSonarr)
        {
            grid.AddRow(new Markup("[aqua]Sonarr URL[/]"), new Markup($"[grey]{opts.SonarrUrl}[/]"));
            if (!string.IsNullOrWhiteSpace(opts.SonarrQueueCleanupCron))
                grid.AddRow(new Markup("[grey]  Queue Cleanup[/]"), new Markup($"[grey]{opts.SonarrQueueCleanupCron}[/]"));
            if (!string.IsNullOrWhiteSpace(opts.SonarrMissingSearchCron))
                grid.AddRow(new Markup("[grey]  Missing Search[/]"), new Markup($"[grey]{opts.SonarrMissingSearchCron}[/]"));
            if (!string.IsNullOrWhiteSpace(opts.SonarrUpgradeSearchCron))
                grid.AddRow(new Markup("[grey]  Upgrade Search[/]"), new Markup($"[grey]{opts.SonarrUpgradeSearchCron}[/]"));
        }

        if (opts.HasRadarr)
        {
            grid.AddRow(new Markup("[yellow]Radarr URL[/]"), new Markup($"[grey]{opts.RadarrUrl}[/]"));
            if (!string.IsNullOrWhiteSpace(opts.RadarrQueueCleanupCron))
                grid.AddRow(new Markup("[grey]  Queue Cleanup[/]"), new Markup($"[grey]{opts.RadarrQueueCleanupCron}[/]"));
            if (!string.IsNullOrWhiteSpace(opts.RadarrMissingSearchCron))
                grid.AddRow(new Markup("[grey]  Missing Search[/]"), new Markup($"[grey]{opts.RadarrMissingSearchCron}[/]"));
            if (!string.IsNullOrWhiteSpace(opts.RadarrUpgradeSearchCron))
                grid.AddRow(new Markup("[grey]  Upgrade Search[/]"), new Markup($"[grey]{opts.RadarrUpgradeSearchCron}[/]"));
        }

        grid.AddRow(new Markup("[grey]Dry Run[/]"), new Markup(opts.IsDryRun ? "[yellow]true[/]" : "[grey]false[/]"));

        var panel = new Panel(grid)
            .Border(BoxBorder.Double)
            .BorderColor(opts.HasSonarr ? Color.Aqua : Color.Yellow)
            .Header(new PanelHeader(" ArrWarden "));

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public void WriteQueueResult(DateTime timestamp, string instance, string job, int blocked, int matched,
        IReadOnlyList<(string Title, string Rule)> items, bool isDryRun)
    {
        var ts = $"[grey]{timestamp:HH:mm:ss}[/]";
        var instanceMarkup = InstanceMarkup(instance);

        if (Interactive)
            WriteQueueResultInteractive(ts, instanceMarkup, job, instance, blocked, matched, items, isDryRun);
        else
            WriteQueueResultPlain(ts, instanceMarkup, job, blocked, matched, items, isDryRun);
    }

    private static void WriteQueueResultInteractive(string ts, string instanceMarkup, string job, string instance,
        int blocked, int matched, IReadOnlyList<(string Title, string Rule)> items, bool isDryRun)
    {
        if (matched == 0)
        {
            var emptyTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(InstanceColor(instance))
                .AddColumn("[grey]Status[/]");
            emptyTable.AddRow($"[grey]blocked[/] [white]{blocked}[/]  no rules activated");

            var emptyPanel = new Panel(emptyTable)
                .Header($"{ts}  {instanceMarkup}  [grey]{job}[/]")
                .BorderColor(InstanceColor(instance));
            AnsiConsole.Write(emptyPanel);
        }
        else
        {
            var verb = isDryRun ? "would blocklist" : "blocklisted";
            var color = isDryRun ? "yellow" : "green";

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn("[grey]Title[/]")
                .AddColumn("[grey]Rule[/]");

            foreach (var (title, rule) in items)
                table.AddRow(Markup.Escape(title), $"[magenta]{Markup.Escape(rule)}[/]");

            if (items.Count < matched)
                table.AddRow($"[grey]+{matched - items.Count} more[/]", "");

            var panel = new Panel(table)
                .Header($"{ts}  {instanceMarkup}  [grey]{job}[/]  [grey]blocked[/] [white]{blocked}[/]  [grey]matched[/] [white]{matched}[/]  [{color}]{verb}[/]")
                .BorderColor(InstanceColor(instance));
            AnsiConsole.Write(panel);
        }
        AnsiConsole.WriteLine();
    }

    private static void WriteQueueResultPlain(string ts, string instanceMarkup, string job, int blocked, int matched,
        IReadOnlyList<(string Title, string Rule)> items, bool isDryRun)
    {
        if (matched == 0)
        {
            AnsiConsole.MarkupLine($"{ts}  {instanceMarkup}  [grey]{job}[/]  blocked [white]{blocked}[/]  no rules activated");
        }
        else
        {
            var verb = isDryRun ? "would blocklist" : "blocklisted";
            var color = isDryRun ? "yellow" : "green";
            AnsiConsole.MarkupLine($"{ts}  {instanceMarkup}  [grey]{job}[/]  blocked [white]{blocked}[/]  matched [white]{matched}[/]  [{color}]{verb}[/]");

            foreach (var (title, rule) in items)
                AnsiConsole.MarkupLine($"    {Markup.Escape(title)}  [magenta]({Markup.Escape(rule)})[/]");

            if (items.Count < matched)
                AnsiConsole.MarkupLine($"    [grey]+{matched - items.Count} more[/]");
        }
        Console.WriteLine();
    }

    public async Task RunSearchWithProgress(string instance, string job, int maxResults,
        Func<SearchProgress, Task> searchLogic)
    {
        if (Interactive)
            await RunInteractiveSearch(instance, job, maxResults, searchLogic);
        else
            await RunPlainSearch(instance, job, maxResults, searchLogic);
    }

    private async Task RunInteractiveSearch(string instance, string job, int maxResults,
        Func<SearchProgress, Task> searchLogic)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[grey]Title[/]");

        string currentPhase = "Starting";

        await AnsiConsole.Live(new Panel(table)
            .Header($"{InstanceMarkup(instance)}  [grey]{job}[/]")
            .BorderColor(InstanceColor(instance)))
            .StartAsync(async ctx =>
            {
                var progress = new SearchProgress
                {
                    Instance = instance,
                    Job = job,
                    MaxResults = maxResults,
                    Table = table,
                    LiveCtx = ctx,
                    SetPhaseAction = (phase) =>
                    {
                        currentPhase = phase;
                        var header = $"[grey]{DateTime.Now:HH:mm:ss}[/]  {InstanceMarkup(instance)}  [grey]{job}[/]  [grey]{phase}[/]";
                        ctx.UpdateTarget(new Panel(table).Header(header).BorderColor(InstanceColor(instance)));
                    },
                    ItemSearchedAction = (title) =>
                    {
                        var escaped = Markup.Escape(title);
                        if (escaped.Length > 100)
                            escaped = escaped[..97] + "...";
                        table.AddRow(escaped);
                        ctx.Refresh();
                    },
                    CompleteAction = (totalWanted, onCooldown, searched) =>
                    {
                        table.Rows.Clear();
                        if (totalWanted == 0)
                            table.AddRow("[grey]no wanted items found[/]");
                        else if (searched == 0)
                            table.AddRow($"[grey]total[/] [white]{totalWanted}[/]  [grey]on cooldown[/] [white]{onCooldown}[/]  [grey]eligible[/] [white]{Math.Max(0, totalWanted - onCooldown)}[/]  [grey]max[/] [white]{maxResults}[/]  [grey]nothing to search[/]");
                        else
                            table.AddRow($"[grey]total[/] [white]{totalWanted}[/]  [grey]on cooldown[/] [white]{onCooldown}[/]  [grey]eligible[/] [white]{Math.Max(0, totalWanted - onCooldown)}[/]  [grey]max[/] [white]{maxResults}[/]  [green]searched[/] [white]{searched}[/]");

                        var header = $"[grey]{DateTime.Now:HH:mm:ss}[/]  {InstanceMarkup(instance)}  [grey]{job}[/]";
                        ctx.UpdateTarget(new Panel(table).Header(header).BorderColor(InstanceColor(instance)));
                    }
                };

                await searchLogic(progress);
                await Task.Delay(1500);
            });
    }

    private async Task RunPlainSearch(string instance, string job, int maxResults,
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
            AnsiConsole.MarkupLine($"[grey]{DateTime.Now:HH:mm:ss}[/]  {InstanceMarkup(instance)}  [grey]{job}[/]  [grey]{Markup.Escape(phase)}[/]");
        };

        progress.ItemSearchedAction = (title) =>
        {
            var escaped = Markup.Escape(title);
            if (escaped.Length > 120)
                escaped = escaped[..117] + "...";
            AnsiConsole.MarkupLine($"  [grey]{escaped}[/]");
        };

        progress.CompleteAction = (totalWanted, onCooldown, searched) =>
        {
            var ts = $"[grey]{DateTime.Now:HH:mm:ss}[/]";
            if (totalWanted == 0)
                AnsiConsole.MarkupLine($"{ts}  {InstanceMarkup(instance)}  [grey]{job}[/]  no wanted items found");
            else if (searched == 0)
                AnsiConsole.MarkupLine($"{ts}  {InstanceMarkup(instance)}  [grey]{job}[/]  total [white]{totalWanted}[/]  on cooldown [white]{onCooldown}[/]  eligible [white]{Math.Max(0, totalWanted - onCooldown)}[/]  max [white]{maxResults}[/]  nothing to search");
            else
                AnsiConsole.MarkupLine($"{ts}  {InstanceMarkup(instance)}  [grey]{job}[/]  total [white]{totalWanted}[/]  on cooldown [white]{onCooldown}[/]  eligible [white]{Math.Max(0, totalWanted - onCooldown)}[/]  max [white]{maxResults}[/]  [green]searched[/] [white]{searched}[/]");
            Console.WriteLine();
        };

        await searchLogic(progress);
    }

    public class SearchProgress
    {
        public string Instance { get; set; } = "";
        public string Job { get; set; } = "";
        public int MaxResults { get; set; }

        internal Table? Table { get; set; }
        internal LiveDisplayContext? LiveCtx { get; set; }
        internal Action<string>? SetPhaseAction { get; set; }
        internal Action<string>? ItemSearchedAction { get; set; }
        internal Action<int, int, int>? CompleteAction { get; set; }

        public void SetPhase(string phase) => SetPhaseAction?.Invoke(phase);

        public void ItemSearched(string title) => ItemSearchedAction?.Invoke(title);

        public void Complete(int totalWanted, int onCooldown, int searched) => CompleteAction?.Invoke(totalWanted, onCooldown, searched);
    }
}
