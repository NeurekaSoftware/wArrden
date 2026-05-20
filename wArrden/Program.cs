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

AppConfig config;
if (File.Exists(configPath))
{
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
}
else
{
    var legacy = LegacyConfigLoader.LoadFromEnv();
    if (legacy is null)
    {
        Console.Error.WriteLine("Error: No configuration found.");
        Console.Error.WriteLine("       Set CONFIG_PATH to a config.yaml file, or use the legacy SONARR_*/RADARR_* environment variables.");
        Environment.Exit(1);
    }
    var legacyErrors = YamlConfigLoader.Validate(legacy);
    if (legacyErrors.Count > 0)
    {
        foreach (var error in legacyErrors)
            Console.Error.WriteLine($"Config error: {error}");
        Environment.Exit(1);
    }
    config = legacy;
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
                    inst.MissingSearch.SearchType ?? "episode", opts.IsDryRun)
                .Cron(inst.MissingSearch.Cron!)
                .PreventOverlapping($"{instanceKey}_missing");
        }

        if (inst.UpgradeSearch?.Enabled == true)
        {
            scheduler
                .ScheduleWithParams<SearchJob>(client, "upgrade", inst.IsSonarr ? "sonarr" : "radarr",
                    inst.UpgradeSearch.MaxResults!.Value, inst.UpgradeSearch.Cooldown!,
                    inst.UpgradeSearch.SearchType ?? "season", opts.IsDryRun)
                .Cron(inst.UpgradeSearch.Cron!)
                .PreventOverlapping($"{instanceKey}_upgrade");
        }

        if (inst.QueueCleanup?.Enabled == true)
        {
            scheduler
                .ScheduleWithParams<QueueJob>(client, inst.IsSonarr ? "sonarr" : "radarr", opts.IsDryRun)
                .Cron(inst.QueueCleanup.Cron!)
                .PreventOverlapping($"{instanceKey}_queue");
        }
    }
});

OutputService.WriteBanner(config, opts);

await host.RunAsync();

static string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);
