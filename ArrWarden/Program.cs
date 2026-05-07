using ArrWarden.Clients;
using ArrWarden.Configuration;
using ArrWarden.Data;
using ArrWarden.Invocables;
using ArrWarden.Services;
using Coravel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var opts = new WardenOptions();

opts.DryRun = GetEnv("DRY_RUN");
opts.DatabasePath = GetEnv("DATABASE_PATH") ?? "data/warden.db";
opts.SonarrUrl = GetEnv("SONARR_URL");
opts.SonarrApiKey = GetEnv("SONARR_API_KEY");
opts.SonarrApiVersion = GetEnv("SONARR_API_VERSION") ?? "3";
opts.SonarrQueueCleanupCron = GetEnv("SONARR_QUEUE_CLEANUP_CRON");
opts.SonarrMissingSearchCron = GetEnv("SONARR_MISSING_SEARCH_CRON");
opts.SonarrMissingCooldownRaw = GetEnv("SONARR_MISSING_COOLDOWN") ?? "30d";
opts.SonarrMissingMaxResults = int.TryParse(GetEnv("SONARR_MISSING_MAX_RESULTS"), out var smr) ? smr : 100;
opts.SonarrUpgradeSearchCron = GetEnv("SONARR_UPGRADE_SEARCH_CRON");
opts.SonarrUpgradeCooldownRaw = GetEnv("SONARR_UPGRADE_COOLDOWN") ?? "30d";
opts.SonarrUpgradeMaxResults = int.TryParse(GetEnv("SONARR_UPGRADE_MAX_RESULTS"), out var sur) ? sur : 50;

opts.RadarrUrl = GetEnv("RADARR_URL");
opts.RadarrApiKey = GetEnv("RADARR_API_KEY");
opts.RadarrApiVersion = GetEnv("RADARR_API_VERSION") ?? "3";
opts.RadarrQueueCleanupCron = GetEnv("RADARR_QUEUE_CLEANUP_CRON");
opts.RadarrMissingSearchCron = GetEnv("RADARR_MISSING_SEARCH_CRON");
opts.RadarrMissingCooldownRaw = GetEnv("RADARR_MISSING_COOLDOWN") ?? "30d";
opts.RadarrMissingMaxResults = int.TryParse(GetEnv("RADARR_MISSING_MAX_RESULTS"), out var rmr) ? rmr : 100;
opts.RadarrUpgradeSearchCron = GetEnv("RADARR_UPGRADE_SEARCH_CRON");
opts.RadarrUpgradeCooldownRaw = GetEnv("RADARR_UPGRADE_COOLDOWN") ?? "30d";
opts.RadarrUpgradeMaxResults = int.TryParse(GetEnv("RADARR_UPGRADE_MAX_RESULTS"), out var rur) ? rur : 50;

var errors = Validate(opts);
if (errors.Count > 0)
{
    foreach (var error in errors)
        Console.Error.WriteLine($"Error: {error}");
    Environment.Exit(1);
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
builder.Services.AddTransient<Func<IArrClient, string, QueueCleanupService>>(sp =>
    (client, prefix) => new QueueCleanupService(client, sp.GetRequiredService<WardenOptions>(), prefix, sp.GetRequiredService<OutputService>()));

if (opts.HasSonarr)
{
    var sonarrClient = (SonarrV3Client)ArrClientFactory.CreateSonarr(opts.SonarrUrl!, opts.SonarrApiKey!, opts.SonarrApiVersion);
    builder.Services.AddSingleton(sonarrClient);
    builder.Services.AddTransient<SonarrQueueCleanupJob>();
    builder.Services.AddTransient<SonarrMissingSearchJob>();
    builder.Services.AddTransient<SonarrUpgradeSearchJob>();
}

if (opts.HasRadarr)
{
    var radarrClient = (RadarrV3Client)ArrClientFactory.CreateRadarr(opts.RadarrUrl!, opts.RadarrApiKey!, opts.RadarrApiVersion);
    builder.Services.AddSingleton(radarrClient);
    builder.Services.AddTransient<RadarrQueueCleanupJob>();
    builder.Services.AddTransient<RadarrMissingSearchJob>();
    builder.Services.AddTransient<RadarrUpgradeSearchJob>();
}

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();
    await db.Database.EnsureCreatedAsync();
}

host.Services.UseScheduler(scheduler =>
{
    if (opts.HasSonarr && !string.IsNullOrWhiteSpace(opts.SonarrQueueCleanupCron))
        scheduler.Schedule<SonarrQueueCleanupJob>().Cron(opts.SonarrQueueCleanupCron).PreventOverlapping("sonarr_qc");

    if (opts.HasSonarr && !string.IsNullOrWhiteSpace(opts.SonarrMissingSearchCron))
        scheduler.Schedule<SonarrMissingSearchJob>().Cron(opts.SonarrMissingSearchCron).PreventOverlapping("sonarr_ms");

    if (opts.HasSonarr && !string.IsNullOrWhiteSpace(opts.SonarrUpgradeSearchCron))
        scheduler.Schedule<SonarrUpgradeSearchJob>().Cron(opts.SonarrUpgradeSearchCron).PreventOverlapping("sonarr_us");

    if (opts.HasRadarr && !string.IsNullOrWhiteSpace(opts.RadarrQueueCleanupCron))
        scheduler.Schedule<RadarrQueueCleanupJob>().Cron(opts.RadarrQueueCleanupCron).PreventOverlapping("radarr_qc");

    if (opts.HasRadarr && !string.IsNullOrWhiteSpace(opts.RadarrMissingSearchCron))
        scheduler.Schedule<RadarrMissingSearchJob>().Cron(opts.RadarrMissingSearchCron).PreventOverlapping("radarr_ms");

    if (opts.HasRadarr && !string.IsNullOrWhiteSpace(opts.RadarrUpgradeSearchCron))
        scheduler.Schedule<RadarrUpgradeSearchJob>().Cron(opts.RadarrUpgradeSearchCron).PreventOverlapping("radarr_us");
});

OutputService.WriteBanner(opts);

await host.RunAsync();

static string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);

static List<string> Validate(WardenOptions opts)
{
    var errors = new List<string>();

    if (!opts.HasSonarr && !opts.HasRadarr)
    {
        errors.Add("At least one instance must be configured. Set SONARR_URL + SONARR_API_KEY and/or RADARR_URL + RADARR_API_KEY.");
        return errors;
    }

    if (opts.HasSonarr)
    {
        if (!IsValidUrl(opts.SonarrUrl))
            errors.Add($"Invalid Sonarr URL: '{opts.SonarrUrl}'. Must be a valid http(s) URL.");

        if (!IsValidApiKey(opts.SonarrApiKey))
            errors.Add("SONARR_API_KEY must be a non-empty string when SONARR_URL is configured.");

        ValidateInstanceCrons(errors, "Sonarr", opts.SonarrQueueCleanupCron, opts.SonarrMissingSearchCron, opts.SonarrUpgradeSearchCron);
    }

    if (opts.HasRadarr)
    {
        if (!IsValidUrl(opts.RadarrUrl))
            errors.Add($"Invalid Radarr URL: '{opts.RadarrUrl}'. Must be a valid http(s) URL.");

        if (!IsValidApiKey(opts.RadarrApiKey))
            errors.Add("RADARR_API_KEY must be a non-empty string when RADARR_URL is configured.");

        ValidateInstanceCrons(errors, "Radarr", opts.RadarrQueueCleanupCron, opts.RadarrMissingSearchCron, opts.RadarrUpgradeSearchCron);
    }

    return errors;
}

static bool IsValidUrl(string? url) =>
    !string.IsNullOrWhiteSpace(url) &&
    Uri.TryCreate(url, UriKind.Absolute, out var u) &&
    (u.Scheme == "http" || u.Scheme == "https");

static bool IsValidApiKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Trim().Length >= 1;

static void ValidateInstanceCrons(List<string> errors, string name, params string?[] crons)
{
    var configured = crons.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c!).ToList();
    if (configured.Count == 0)
    {
        errors.Add($"At least one {name} cron schedule must be configured (e.g. {name}_QUEUE_CLEANUP_CRON, {name}_MISSING_SEARCH_CRON, or {name}_UPGRADE_SEARCH_CRON).");
        return;
    }

    foreach (var cron in configured)
    {
        if (!IsValidCron(cron))
            errors.Add($"Invalid {name} cron expression: '{cron}'. Must be 5-field cron: 'min hour dom month dow'.");
    }
}

static bool IsValidCron(string cron)
{
    if (string.IsNullOrWhiteSpace(cron)) return false;
    var parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length == 5;
}
