using wArrden.Configuration;

namespace wArrden.Tests;

public class YamlConfigLoaderTests
{
    [Fact]
    public void Validate_EmptyInstances_ReturnsError()
    {
        var config = new AppConfig();
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("No instances defined"));
    }

    [Fact]
    public void Validate_ValidSonarrInstance_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InvalidType_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "lidarr",
                    Name = "Music",
                    Url = "http://localhost:8686",
                    ApiKey = "abc123"
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("type") && e.Contains("sonarr") && e.Contains("radarr"));
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123"
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("name") && e.Contains("required"));
    }

    [Fact]
    public void Validate_InvalidUrl_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "not-a-url",
                    ApiKey = "abc123"
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("url") && e.Contains("http"));
    }

    [Fact]
    public void Validate_MissingApiKey_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = ""
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("apiKey") && e.Contains("required"));
    }

    [Fact]
    public void Validate_InvalidApiVersion_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    ApiVersion = "4"
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("apiVersion") && e.Contains("3"));
    }

    [Fact]
    public void Validate_JobEnabledMissingCron_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("cron") && e.Contains("required"));
    }

    [Fact]
    public void Validate_JobInvalidCron_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "bad cron" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("cron") && e.Contains("5-field"));
    }

    [Fact]
    public void Validate_InvalidCooldown_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", Cooldown = "xyz" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("cooldown"));
    }

    [Fact]
    public void Validate_DuplicateNamesPerType_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                },
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_NegativeMaxResults_PassesSinceZeroIsAllowed()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = -1 }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("maxResults"));
    }

    [Fact]
    public void Validate_TypeCaseInsensitive_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "RADARR",
                    Name = "Movies",
                    Url = "http://localhost:7878",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "* * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DisabledJobWithoutCron_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Series",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = false, Cron = "" },
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }
}
