using YamlDotNet.Core;
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
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            config = deserializer.Deserialize<AppConfig>(yaml) ?? new AppConfig();
        }
        catch (YamlException ex)
        {
            var start = ex.Start;
            var loc = $"line {start.Line}, column {start.Column}";
            throw new ConfigurationException(new List<string>
            {
                $"YAML parse error at {loc}: {ex.InnerException?.Message ?? ex.Message}"
            });
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
            if (inst.MissingSearch is not null && inst.IsSonarr)
            {
                if (string.IsNullOrWhiteSpace(inst.MissingSearch.SearchType))
                {
                    Console.Error.WriteLine(
                        $"Warning: instances '{inst.Name}'.missingSearch: 'searchType' not configured, defaulting to 'episode'.");
                    inst.MissingSearch.SearchType = "episode";
                }
                else
                {
                    inst.MissingSearch.SearchType = inst.MissingSearch.SearchType.ToLowerInvariant();
                }
            }

            if (inst.UpgradeSearch is not null && inst.IsSonarr)
            {
                if (string.IsNullOrWhiteSpace(inst.UpgradeSearch.SearchType))
                {
                    Console.Error.WriteLine(
                        $"Warning: instances '{inst.Name}'.upgradeSearch: 'searchType' not configured, defaulting to 'season'.");
                    inst.UpgradeSearch.SearchType = "season";
                }
                else
                {
                    inst.UpgradeSearch.SearchType = inst.UpgradeSearch.SearchType.ToLowerInvariant();
                }
            }
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

        ValidateQueueCleanupRules(errors, config.QueueCleanupRules);

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

        if (job.Enabled is null)
            errors.Add($"{prefix}: 'enabled' is required.");

        if (job.Cron is null)
            errors.Add($"{prefix}: 'cron' is required.");
        else if (job.Enabled == true && !IsValidCron(job.Cron))
            errors.Add($"{prefix}: 'cron' must be a 5-field expression 'min hour dom month dow'.");

        if (jobKey != "queueCleanup")
        {
            if (job.MaxResults is null)
                errors.Add($"{prefix}: 'maxResults' is required.");
            else if (job.MaxResults < 0)
                errors.Add($"{prefix}: 'maxResults' must be 0 or greater.");

            if (job.Cooldown is null)
                errors.Add($"{prefix}: 'cooldown' is required.");
            else
            {
                try { DurationParser.Parse(job.Cooldown); }
                catch (Exception ex) { errors.Add($"{prefix}: invalid 'cooldown' - {ex.Message}"); }
            }
        }

        if (inst.IsSonarr && jobKey != "queueCleanup" && !string.IsNullOrWhiteSpace(job.SearchType))
        {
            var st = job.SearchType;
            if (!string.Equals(st, "episode", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(st, "season", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{prefix}: 'searchType' must be 'episode' or 'season'.");
            }
        }

        if (inst.IsRadarr && jobKey != "queueCleanup" && !string.IsNullOrWhiteSpace(job.SearchType))
        {
            errors.Add($"{prefix}: 'searchType' is not valid for Radarr instances.");
        }
    }

    private static void ValidateQueueCleanupRules(List<string> errors, QueueCleanupRulesConfig? rules)
    {
        if (rules is null) return;

        ValidateRuleList(errors, rules.Sonarr, "sonarr");
        ValidateRuleList(errors, rules.Radarr, "radarr");
    }

    private static void ValidateRuleList(List<string> errors, List<QueueCleanupRuleConfig>? list, string type)
    {
        if (list is null || list.Count == 0)
        {
            Console.Error.WriteLine($"Warning: queueCleanupRules.{type} is empty; no queue warnings will be matched for {type} instances.");
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var prefix = $"queueCleanupRules.{type}[{i}]";
            var rule = list[i];

            if (string.IsNullOrWhiteSpace(rule.Match))
                errors.Add($"{prefix}: 'match' must not be empty.");

            var action = rule.Action?.Trim();
            if (string.IsNullOrWhiteSpace(action))
            {
                errors.Add($"{prefix}: 'action' is required.");
            }
            else if (!string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(action, "removeAndBlocklist", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{prefix}: 'action' must be 'remove' or 'removeAndBlocklist', got '{rule.Action}'.");
            }
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
