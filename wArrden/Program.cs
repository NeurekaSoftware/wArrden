using wArrden.Clients;
using wArrden.Configuration;
using wArrden.Data;
using wArrden.Invocables;
using wArrden.Services;
using Coravel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var opts = new WardenOptions
{
    DryRun = GetEnv("DRY_RUN"),
    Timezone = GetEnv("TZ"),
    DatabasePath = GetEnv("DATABASE_PATH") ?? "data/warden.db"
};

var configPath = GetEnv("CONFIG_PATH") ?? "data/config.yaml";

if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Error: Config file not found: {configPath}");
    Environment.Exit(1);
    return;
}

AppConfig config;
try
{
    config = YamlConfigLoader.Load(configPath);
}
catch (ConfigurationException ex)
{
    foreach (var error in ex.Errors)
        Console.Error.WriteLine($"Config error: {error}");
    Environment.Exit(1);
    return;
}

var startupOutput = new OutputService { MinimumLevel = ParseLogLevel(config.LogLevel) };
var logLevelLabel = config.LogLevel ?? "info";
startupOutput.WriteDebug("warden.config", $"Loaded config from {configPath}");
startupOutput.WriteDebug("warden.config", $"Log level set to {logLevelLabel}");

var dbPath = Path.GetFullPath(opts.DatabasePath);
var dbDir = Path.GetDirectoryName(dbPath);
if (dbDir is not null)
    Directory.CreateDirectory(dbDir);

startupOutput.WriteDebug("warden.config", $"Database path: {dbPath}");

if (args.Length > 0)
{
    var command = args[0];

    if (command is "clear-missing" or "clear-upgrades")
    {
        var category = command == "clear-missing" ? "Missing" : "Upgrade";
        var targets = ResolveTargets(config, args.Length > 1 ? args[1] : null, startupOutput);
        if (targets is null) return;

        await RunClearCooldownsCommand(dbPath, category, targets, opts, startupOutput);
        return;
    }

    startupOutput.WriteError("cli", $"Unknown command: {args[0]}");
    startupOutput.WriteError("cli", "Available commands: clear-missing [instance], clear-upgrades [instance]");
    Environment.Exit(1);
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

builder.Services.AddDbContext<WardenDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton(opts);
builder.Services.AddScheduler();
builder.Services.AddSingleton<ICooldownService, CooldownService>();
builder.Services.AddSingleton(new OutputService { MinimumLevel = ParseLogLevel(config.LogLevel) });
builder.Services.AddSingleton<SearchService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
}

var schedulerOutput = host.Services.GetRequiredService<OutputService>();
var clients = new List<IArrClient>();

host.Services.UseScheduler(scheduler =>
{
    foreach (var inst in config.Instances)
    {
        var client = inst switch
        {
            { IsSonarr: true } => ArrClientFactory.CreateSonarr(inst.Url, inst.ApiKey, inst.ApiVersion, inst.Name),
            { IsRadarr: true } => ArrClientFactory.CreateRadarr(inst.Url, inst.ApiKey, inst.ApiVersion, inst.Name),
            { IsLidarr: true } => ArrClientFactory.CreateLidarr(inst.Url, inst.ApiKey, inst.ApiVersion, inst.Name),
            { IsWhisparr: true } => ArrClientFactory.CreateWhisparr(inst.Url, inst.ApiKey, inst.ApiVersion, inst.Name),
            _ => throw new InvalidOperationException($"Unknown instance type: {inst.Type}")
        };
        clients.Add(client);

        var instanceKey = inst.InstanceKey;
        var instanceType = InstanceType(inst);

        schedulerOutput.WriteDebug("warden.scheduler", $"Scheduling jobs for {inst.Name} ({instanceType})");

        if (inst.MissingSearch?.Enabled == true)
        {
            scheduler
                .ScheduleWithParams<SearchJob>(client, "missing", instanceType,
                    inst.MissingSearch.MaxResults!.Value, inst.MissingSearch.Cooldown!,
                    inst.MissingSearch.SearchType ?? "", opts.IsDryRun, inst.IndexerNames ?? new List<string>())
                .Cron(inst.MissingSearch.Cron!)
                .PreventOverlapping($"{instanceKey}_missing");
        }

        if (inst.UpgradeSearch?.Enabled == true)
        {
            scheduler
                .ScheduleWithParams<SearchJob>(client, "upgrade", instanceType,
                    inst.UpgradeSearch.MaxResults!.Value, inst.UpgradeSearch.Cooldown!,
                    inst.UpgradeSearch.SearchType ?? "", opts.IsDryRun, inst.IndexerNames ?? new List<string>())
                .Cron(inst.UpgradeSearch.Cron!)
                .PreventOverlapping($"{instanceKey}_upgrade");
        }

        if (inst.QueueCleanup?.Enabled == true)
        {
            var rules = GetRulesForType(config.QueueCleanupRules, instanceType);
            if (rules is null)
            {
                schedulerOutput.WriteWarning($"{instanceKey}.queue", "Queue cleanup is enabled but no rules are configured for this instance type — job will not be scheduled.");
                return;
            }

            scheduler
                .ScheduleWithParams<QueueJob>(client, instanceType, opts.IsDryRun, rules)
                .Cron(inst.QueueCleanup.Cron!)
                .PreventOverlapping($"{instanceKey}_queue");
        }
    }
})
.OnError(ex =>
{
    schedulerOutput.WriteError("warden.scheduler", "Scheduled task error", ex);
});

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    foreach (var client in clients)
        client.Dispose();
});

OutputService.WriteBanner(config, opts);

await host.RunAsync();

static string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);

static wArrden.Services.LogLevel ParseLogLevel(string? level)
{
    if (string.IsNullOrWhiteSpace(level)) return wArrden.Services.LogLevel.Info;
    return level.Trim().Equals("debug", StringComparison.OrdinalIgnoreCase) ? wArrden.Services.LogLevel.Debug :
           level.Trim().Equals("warning", StringComparison.OrdinalIgnoreCase) ? wArrden.Services.LogLevel.Warning :
           level.Trim().Equals("error", StringComparison.OrdinalIgnoreCase) ? wArrden.Services.LogLevel.Error :
           wArrden.Services.LogLevel.Info;
}

static string InstanceType(InstanceConfig inst)
{
    if (inst.IsSonarr) return "sonarr";
    if (inst.IsRadarr) return "radarr";
    if (inst.IsLidarr) return "lidarr";
    if (inst.IsWhisparr) return "whisparr";
    throw new InvalidOperationException($"Unknown instance type: {inst.Type}");
}

static List<QueueCleanupRule>? GetRulesForType(QueueCleanupRulesConfig? config, string type)
{
    var list = type switch
    {
        "sonarr" => config?.Sonarr,
        "radarr" => config?.Radarr,
        "lidarr" => config?.Lidarr,
        "whisparr" => config?.Whisparr,
        _ => null
    };
    if (list is null || list.Count == 0) return null;
    return list
        .Where(r => !string.Equals(r.Action, "none", StringComparison.OrdinalIgnoreCase))
        .Select(r => new QueueCleanupRule(
            r.Match,
            string.Equals(r.Action, "removeAndBlocklist", StringComparison.OrdinalIgnoreCase)
        ))
        .ToList();
}

static List<InstanceConfig>? ResolveTargets(AppConfig config, string? instanceArg, OutputService output)
{
    if (instanceArg is null)
        return config.Instances;

    var match = config.Instances.FirstOrDefault(i =>
        string.Equals(i.Name, instanceArg, StringComparison.OrdinalIgnoreCase));
    if (match is not null)
        return new List<InstanceConfig> { match };

    output.WriteError("cli", $"Unknown instance: {instanceArg}");
    output.WriteError("cli", $"Available instances: {string.Join(", ", config.Instances.Select(i => i.Name))}");
    Environment.Exit(1);
    return null;
}

static async Task RunClearCooldownsCommand(string dbPath, string category, List<InstanceConfig> targets, WardenOptions opts, OutputService output)
{
    var services = new ServiceCollection();
    services.AddDbContext<WardenDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
    services.AddSingleton(output);
    services.AddSingleton<ICooldownService, CooldownService>();

    var sp = services.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");

    var cooldown = scope.ServiceProvider.GetRequiredService<ICooldownService>();

    var counts = new List<(string Instance, int Count)>();
    foreach (var inst in targets)
    {
        var count = await cooldown.ClearAllAsync(category, inst.Name, CancellationToken.None);
        counts.Add((inst.Name, count));
        output.WriteDebug($"cli.{targets[0].Name.ToLowerInvariant()}.clear", $"Cleared {count} cooldown entries for {inst.Name}");
    }

    var tz = ResolveTimezone(opts.Timezone);
    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    var ts = FormatTimestamp(now);

    var isSingle = targets.Count == 1;
    var jobKey = category == "Missing" ? "clear-missing" : "clear-upgrades";
    var headerLabel = isSingle
        ? $"cli.{targets[0].Name.ToLowerInvariant()}.{jobKey}"
        : $"cli.{jobKey}";

    Console.WriteLine($"[{ts} INFO] [{headerLabel}]");

    var typeLabel = category == "Missing" ? "Missing" : "Upgrade";
    var lines = new List<string> { $" ├─ Type:       {typeLabel}" };

    if (isSingle)
    {
        var c = counts[0].Count;
        lines.Add($" └─ Cleared:    {c} entr{(c == 1 ? "y" : "ies")}");
    }
    else
    {
        foreach (var (inst, count) in counts)
            lines.Add($" ├─ {inst}:     {count} entr{(count == 1 ? "y" : "ies")}");

        var total = counts.Sum(x => x.Count);
        lines.Add($" └─ Cleared:    {total} entr{(total == 1 ? "y" : "ies")}");
    }

    foreach (var line in lines)
        Console.WriteLine(line);

    Console.WriteLine();
}

static TimeZoneInfo ResolveTimezone(string? tzId)
{
    if (!string.IsNullOrWhiteSpace(tzId))
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
        catch { }
    }
    return TimeZoneInfo.Local;
}

static string FormatTimestamp(DateTime dt) => dt.ToString("MM/dd/yyyy hh:mm:ss tt");
