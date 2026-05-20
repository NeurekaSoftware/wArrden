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

var dbPath = Path.GetFullPath(opts.DatabasePath);
var dbDir = Path.GetDirectoryName(dbPath);
if (dbDir is not null)
    Directory.CreateDirectory(dbDir);

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddDbContext<WardenDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton(opts);
builder.Services.AddScheduler();
builder.Services.AddSingleton<ICooldownService, CooldownService>();
builder.Services.AddSingleton<OutputService>();
builder.Services.AddSingleton<SearchService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();
    await db.Database.EnsureCreatedAsync();
}

host.Services.UseScheduler(scheduler =>
{
    foreach (var inst in config.Instances)
    {
        var client = inst.IsSonarr
            ? ArrClientFactory.CreateSonarr(inst.Url, inst.ApiKey, inst.ApiVersion, inst.Name)
            : ArrClientFactory.CreateRadarr(inst.Url, inst.ApiKey, inst.ApiVersion, inst.Name);

        var instanceKey = inst.InstanceKey;

        if (inst.MissingSearch?.Enabled == true)
        {
            scheduler
                .ScheduleWithParams<SearchJob>(client, "missing", inst.IsSonarr ? "sonarr" : "radarr",
                    inst.MissingSearch.MaxResults!.Value, inst.MissingSearch.Cooldown!,
                    inst.MissingSearch.SearchType!, opts.IsDryRun)
                .Cron(inst.MissingSearch.Cron!)
                .PreventOverlapping($"{instanceKey}_missing");
        }

        if (inst.UpgradeSearch?.Enabled == true)
        {
            scheduler
                .ScheduleWithParams<SearchJob>(client, "upgrade", inst.IsSonarr ? "sonarr" : "radarr",
                    inst.UpgradeSearch.MaxResults!.Value, inst.UpgradeSearch.Cooldown!,
                    inst.UpgradeSearch.SearchType!, opts.IsDryRun)
                .Cron(inst.UpgradeSearch.Cron!)
                .PreventOverlapping($"{instanceKey}_upgrade");
        }

        if (inst.QueueCleanup?.Enabled == true)
        {
            var rules = GetRulesForType(config.QueueCleanupRules, inst.IsSonarr ? "sonarr" : "radarr");
            scheduler
                .ScheduleWithParams<QueueJob>(client, inst.IsSonarr ? "sonarr" : "radarr", opts.IsDryRun, rules)
                .Cron(inst.QueueCleanup.Cron!)
                .PreventOverlapping($"{instanceKey}_queue");
        }
    }
});

OutputService.WriteBanner(config, opts);

await host.RunAsync();

static string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);

static List<QueueCleanupRule>? GetRulesForType(QueueCleanupRulesConfig? config, string type)
{
    var list = type == "sonarr" ? config?.Sonarr : config?.Radarr;
    if (list is null || list.Count == 0) return null;
    return list.Select(r => new QueueCleanupRule(
        r.Match,
        string.Equals(r.Action, "removeAndBlocklist", StringComparison.OrdinalIgnoreCase)
    )).ToList();
}
