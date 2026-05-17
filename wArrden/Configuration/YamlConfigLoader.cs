using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace wArrden.Configuration;

internal static class YamlConfigLoader
{
    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}");

        var yaml = File.ReadAllText(path);
        AppConfig config;

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            config = deserializer.Deserialize<AppConfig>(yaml) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse config file '{path}': {ex.Message}", ex);
        }

        var errors = Validate(config);
        if (errors.Count > 0)
            throw new ConfigurationException(errors);

        ApplyDefaults(config);

        return config;
    }

    private static void ApplyDefaults(AppConfig config)
    {
        foreach (var inst in config.Instances)
        {
            if (inst.MissingSearch is not null && inst.MissingSearch.MaxResults == 0)
                inst.MissingSearch.MaxResults = 100;
            if (inst.UpgradeSearch is not null && inst.UpgradeSearch.MaxResults == 0)
                inst.UpgradeSearch.MaxResults = 50;
        }
    }

    internal static List<string> Validate(AppConfig config)
    {
        var errors = new List<string>();

        if (config.Instances is null || config.Instances.Count == 0)
        {
            errors.Add("No instances defined. The 'instances' list must contain at least one entry.");
            return errors;
        }

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < config.Instances.Count; i++)
        {
            var inst = config.Instances[i];
            var prefix = $"instances[{i}]";

            if (string.IsNullOrWhiteSpace(inst.Name))
            {
                errors.Add($"{prefix}: 'name' is required.");
            }
            else
            {
                if (inst.IsSonarr || inst.IsRadarr) TrackName(usedNames, inst.Name, errors);
                else
                    errors.Add($"{prefix} '{inst.Name}': 'type' must be 'sonarr' or 'radarr'.");
            }

            if (string.IsNullOrWhiteSpace(inst.Type))
                errors.Add($"{prefix}: 'type' is required.");
            else if (!inst.IsSonarr && !inst.IsRadarr)
                errors.Add($"{prefix} '{inst.Name}': 'type' must be 'sonarr' or 'radarr'.");

            if (!IsValidUrl(inst.Url))
                errors.Add($"{prefix} '{inst.Name}': 'url' must be a valid http(s) URL.");

            if (string.IsNullOrWhiteSpace(inst.ApiKey))
                errors.Add($"{prefix} '{inst.Name}': 'apiKey' is required.");

            if (inst.ApiVersion != "3")
                errors.Add($"{prefix} '{inst.Name}': 'apiVersion' must be '3'.");

            ValidateJob(errors, inst, "missingSearch", i);
            ValidateJob(errors, inst, "upgradeSearch", i);
            ValidateJob(errors, inst, "queueCleanup", i);
        }

        var enabledJobs = config.Instances.Sum(i =>
            (i.MissingSearch?.Enabled == true ? 1 : 0) +
            (i.UpgradeSearch?.Enabled == true ? 1 : 0) +
            (i.QueueCleanup?.Enabled == true ? 1 : 0));

        if (enabledJobs == 0)
            Console.Error.WriteLine("Warning: No jobs are enabled across any instance.");

        return errors;
    }

    private static void ValidateJob(List<string> errors, InstanceConfig inst, string jobKey, int idx)
    {
        var job = jobKey switch
        {
            "missingSearch" => inst.MissingSearch,
            "upgradeSearch" => inst.UpgradeSearch,
            "queueCleanup" => inst.QueueCleanup,
            _ => null
        };

        if (job is null) return;

        var prefix = $"instances[{idx}] '{inst.Name}'.{jobKey}";

        if (job.Enabled)
        {
            if (string.IsNullOrWhiteSpace(job.Cron))
                errors.Add($"{prefix}: 'cron' is required when enabled.");
            else if (!IsValidCron(job.Cron))
                errors.Add($"{prefix}: 'cron' must be a 5-field expression 'min hour dom month dow'.");

            if (jobKey != "queueCleanup" && job.MaxResults < 0)
                errors.Add($"{prefix}: 'maxResults' must be 0 or greater.");
        }

        if (!string.IsNullOrWhiteSpace(job.Cooldown))
        {
            try { DurationParser.Parse(job.Cooldown); }
            catch (Exception ex) { errors.Add($"{prefix}: invalid 'cooldown' - {ex.Message}"); }
        }
    }

    private static void TrackName(HashSet<string> usedNames, string name, List<string> errors)
    {
        if (!usedNames.Add(name))
            errors.Add($"Duplicate instance name: '{name}'. Instance names must be unique.");
    }

    private static bool IsValidUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        (u.Scheme == "http" || u.Scheme == "https");

    private static bool IsValidCron(string cron)
    {
        if (string.IsNullOrWhiteSpace(cron)) return false;
        var parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 5;
    }
}
