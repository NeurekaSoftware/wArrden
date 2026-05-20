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
                    MissingSearch = new JobConfig { Enabled = true, MaxResults = 10, Cooldown = "30d" }
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "xyz" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("cooldown"));
    }

    [Fact]
    public void Validate_DuplicateNames_ReturnsError()
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
    public void Validate_DuplicateNamesCrossType_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr",
                    Name = "Movies",
                    Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                },
                new()
                {
                    Type = "radarr",
                    Name = "Movies",
                    Url = "http://localhost:7878",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "* * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_NegativeMaxResults_ReturnsError()
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = -1, Cooldown = "30d" }
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
                    MissingSearch = new JobConfig { Enabled = false, Cron = "0 0 * * *", MaxResults = 0, Cooldown = "30d" },
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            YamlConfigLoader.Load("nonexistent_file.yaml"));
    }

    [Fact]
    public void Load_InvalidYaml_ThrowsConfigurationException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{{{ invalid yaml :::");

            Assert.Throws<ConfigurationException>(() =>
                YamlConfigLoader.Load(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_ValidationErrors_ThrowsConfigurationException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "instances:\n  - type: sonarr\n    name: ''\n    url: http://localhost:8989\n    apiKey: abc123");

            var ex = Assert.Throws<ConfigurationException>(() =>
                YamlConfigLoader.Load(tempFile));

            Assert.NotEmpty(ex.Errors);
            Assert.Contains(ex.Errors, e => e.Contains("name"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_AllKeysPresent_LoadsSuccessfully()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"
instances:
  - type: sonarr
    name: Series
    url: http://localhost:8989
    apiKey: abc123
    missingSearch:
      enabled: true
      cron: '*/5 * * * *'
      maxResults: 15
      cooldown: 30d
    upgradeSearch:
      enabled: true
      cron: '*/10 * * * *'
      maxResults: 15
      cooldown: 30d
    queueCleanup:
      enabled: true
      cron: '*/5 * * * *'
");

            var config = YamlConfigLoader.Load(tempFile);

            var inst = config.Instances[0];
            Assert.Equal(15, inst.MissingSearch!.MaxResults);
            Assert.Equal(15, inst.UpgradeSearch!.MaxResults);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_SonarrMissingSearch_ValidSeasonSearchType_Passes()
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "season" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_SonarrUpgradeSearch_ValidEpisodeSearchType_Passes()
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
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "episode" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_SonarrSearch_InvalidSearchType_ReturnsError()
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "series" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("episode") && e.Contains("season"));
    }

    [Fact]
    public void Validate_SonarrSearch_SearchTypeCaseInsensitive_Passes()
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "SEASON" },
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "Episode" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RadarrSearch_SearchTypeSet_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "radarr",
                    Name = "Movies",
                    Url = "http://localhost:7878",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "season" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("Radarr"));
    }

    [Fact]
    public void Validate_RadarrSearch_NoSearchTypeSet_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "radarr",
                    Name = "Movies",
                    Url = "http://localhost:7878",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Load_SonarrMissingSearch_SearchTypeDefaultsToEpisode_WithWarning()
    {
        var tempFile = Path.GetTempFileName();
        var stderr = new StringWriter();
        var originalError = Console.Error;
        Console.SetError(stderr);
        try
        {
            File.WriteAllText(tempFile, @"
instances:
  - type: sonarr
    name: Series
    url: http://localhost:8989
    apiKey: abc123
    missingSearch:
      enabled: true
      cron: '*/5 * * * *'
      maxResults: 10
      cooldown: 30d
    queueCleanup:
      enabled: true
      cron: '* * * * *'
");

            var config = YamlConfigLoader.Load(tempFile);

            var inst = config.Instances[0];
            Assert.Equal("episode", inst.MissingSearch!.SearchType);
        }
        finally
        {
            Console.SetError(originalError);
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_SonarrUpgradeSearch_SearchTypeDefaultsToSeason_WithWarning()
    {
        var tempFile = Path.GetTempFileName();
        var originalError = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            File.WriteAllText(tempFile, @"
instances:
  - type: sonarr
    name: Series
    url: http://localhost:8989
    apiKey: abc123
    upgradeSearch:
      enabled: true
      cron: '*/5 * * * *'
      maxResults: 10
      cooldown: 30d
    queueCleanup:
      enabled: true
      cron: '* * * * *'
");

            var config = YamlConfigLoader.Load(tempFile);

            var inst = config.Instances[0];
            Assert.Equal("season", inst.UpgradeSearch!.SearchType);
        }
        finally
        {
            Console.SetError(originalError);
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_MissingEnabled_ReturnsError()
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
                    MissingSearch = new JobConfig { Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("enabled") && e.Contains("required"));
    }

    [Fact]
    public void Validate_MissingCron_ReturnsError()
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
                    MissingSearch = new JobConfig { Enabled = true, MaxResults = 10, Cooldown = "30d" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("cron") && e.Contains("required"));
    }

    [Fact]
    public void Validate_MissingMaxResults_ReturnsError()
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", Cooldown = "30d" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("maxResults") && e.Contains("required"));
    }

    [Fact]
    public void Validate_MissingCooldown_ReturnsError()
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10 }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("cooldown") && e.Contains("required"));
    }

    [Fact]
    public void Validate_MissingSearchType_PassesAndDefaults()
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
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d" },
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "* * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Load_UnknownJobKey_ThrowsYamlParseError()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"
instances:
  - type: sonarr
    name: Series
    url: http://localhost:8989
    apiKey: abc123
    missingSearch:
      enabled: true
      cron: '*/5 * * * *'
      maxResults: 10
      cooldown: 30d
      badKey: value
    queueCleanup:
      enabled: true
      cron: '* * * * *'
");

            var ex = Assert.Throws<ConfigurationException>(() =>
                YamlConfigLoader.Load(tempFile));

            Assert.Contains(ex.Errors, e => e.Contains("YAML parse error"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_UnknownInstanceKey_ThrowsYamlParseError()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"
instances:
  - type: sonarr
    name: Series
    url: http://localhost:8989
    apiKey: abc123
    badKey: value
    queueCleanup:
      enabled: true
      cron: '* * * * *'
");

            var ex = Assert.Throws<ConfigurationException>(() =>
                YamlConfigLoader.Load(tempFile));

            Assert.Contains(ex.Errors, e => e.Contains("YAML parse error"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_QueueCleanup_DoesNotRequireMaxResultsOrCooldown()
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
    public void Load_ConfigExampleYaml_LoadsAndPassesValidation()
    {
        var examplePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config.example.yaml");
        Assert.True(File.Exists(examplePath), $"config.example.yaml not found at {examplePath}");

        var config = YamlConfigLoader.Load(examplePath);

        Assert.NotNull(config);
        Assert.NotEmpty(config.Instances);
        Assert.Equal(3, config.Instances.Count);
    }
}
