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
                    Type = "unknown",
                    Name = "Music",
                    Url = "http://localhost:8686",
                    ApiKey = "abc123"
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("type") && e.Contains("sonarr") && e.Contains("radarr") && e.Contains("lidarr") && e.Contains("whisparr"));
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
                    MissingSearch = new JobConfig { Enabled = false, Cron = "0 0 * * *", MaxResults = 0, Cooldown = "30d", SearchType = "episode" },
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
      searchType: episode
    upgradeSearch:
      enabled: true
      cron: '*/10 * * * *'
      maxResults: 15
      cooldown: 30d
      searchType: season
    queueCleanup:
      enabled: true
      cron: '*/5 * * * *'
");

            var config = YamlConfigLoader.Load(tempFile);

            var inst = config.Instances[0];
            Assert.Equal(15, inst.MissingSearch!.MaxResults);
            Assert.Equal("episode", inst.MissingSearch.SearchType);
            Assert.Equal(15, inst.UpgradeSearch!.MaxResults);
            Assert.Equal("season", inst.UpgradeSearch.SearchType);
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

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("radarr"));
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
    public void Load_SonarrMissingSearch_MissingSearchType_ThrowsConfigurationException()
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
    queueCleanup:
      enabled: true
      cron: '* * * * *'
");

            var ex = Assert.Throws<ConfigurationException>(() =>
                YamlConfigLoader.Load(tempFile));

            Assert.Contains(ex.Errors, e => e.Contains("searchType") && e.Contains("required"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_SonarrUpgradeSearch_MissingSearchType_ThrowsConfigurationException()
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
    upgradeSearch:
      enabled: true
      cron: '*/5 * * * *'
      maxResults: 10
      cooldown: 30d
    queueCleanup:
      enabled: true
      cron: '* * * * *'
");

            var ex = Assert.Throws<ConfigurationException>(() =>
                YamlConfigLoader.Load(tempFile));

            Assert.Contains(ex.Errors, e => e.Contains("searchType") && e.Contains("required"));
        }
        finally
        {
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
    public void Validate_MissingSearchType_ReturnsError()
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

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("required"));
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
        Assert.Equal(4, config.Instances.Count);
        Assert.NotNull(config.QueueCleanupRules);
        Assert.NotNull(config.QueueCleanupRules!.Sonarr);
        Assert.NotNull(config.QueueCleanupRules.Radarr);
        Assert.NotNull(config.QueueCleanupRules.Lidarr);
        Assert.NotNull(config.QueueCleanupRules.Whisparr);
        Assert.Equal(27, config.QueueCleanupRules.Sonarr!.Count);
        Assert.Equal(18, config.QueueCleanupRules.Radarr!.Count);
        Assert.Equal(24, config.QueueCleanupRules.Lidarr!.Count);
        Assert.Equal(27, config.QueueCleanupRules.Whisparr!.Count);
    }

    [Fact]
    public void Validate_QueueCleanupRules_AllValid_Passes()
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
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Sonarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "removeAndBlocklist" },
                    new() { Match = "Not an upgrade", Action = "remove" }
                },
                Radarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "removeAndBlocklist" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_QueueCleanupRules_EmptyMatch_ReturnsError()
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
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Sonarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("match") && e.Contains("empty"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_WhitespaceMatch_ReturnsError()
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
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Sonarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "   ", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("match") && e.Contains("empty"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_InvalidAction_ReturnsError()
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
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Sonarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "delete" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("action") && e.Contains("remove") && e.Contains("removeAndBlocklist"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_EmptyAction_ReturnsError()
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
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Sonarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("action") && e.Contains("required"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_MissingSection_Passes()
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
    public void Validate_QueueCleanupRules_EmptySonarrList_Warns()
    {
        var originalError = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
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
                },
                QueueCleanupRules = new QueueCleanupRulesConfig { Sonarr = new List<QueueCleanupRuleConfig>() }
            };
            var errors = YamlConfigLoader.Validate(config);

            Assert.Empty(errors);
            var warning = stderr.ToString();
            Assert.Contains("queueCleanupRules.sonarr", warning);
            Assert.Contains("empty", warning);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Validate_LidarrInstance_Passes()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhisparrInstance_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhisparrSearch_ValidSearchType_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "episode" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhisparrSearch_MissingSearchType_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d" },
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "* * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("required"));
    }

    [Fact]
    public void Validate_LidarrSearch_InvalidSearchType_ReturnsError()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "episode" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("album") && e.Contains("artist"));
    }

    [Fact]
    public void Validate_TypeCaseInsensitive_LidarrWhisparr_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "LIDARR",
                    Name = "Music",
                    Url = "http://localhost:8686",
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "album" },
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                },
                new()
                {
                    Type = "Whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "episode" },
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Load_LidarrWhisparrQueueCleanupRules_DeserializesCorrectly()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"
instances:
  - type: lidarr
    name: Music
    url: http://localhost:8686
    apiKey: abc123
    apiVersion: '1'
    queueCleanup:
      enabled: true
      cron: '* * * * *'
  - type: whisparr
    name: Adult
    url: http://localhost:6969
    apiKey: abc123
    queueCleanup:
      enabled: true
      cron: '* * * * *'
queueCleanupRules:
  lidarr:
    - match: Not an upgrade for existing track file
      action: remove
    - match: Sample
      action: removeAndBlocklist
  whisparr:
    - match: Not an upgrade for existing episode
      action: remove
    - match: Sample
      action: removeAndBlocklist
");

            var config = YamlConfigLoader.Load(tempFile);

            Assert.NotNull(config.QueueCleanupRules);
            Assert.Equal(2, config.QueueCleanupRules!.Lidarr!.Count);
            Assert.Equal("Not an upgrade for existing track file", config.QueueCleanupRules.Lidarr[0].Match);
            Assert.Equal("remove", config.QueueCleanupRules.Lidarr[0].Action);
            Assert.Equal(2, config.QueueCleanupRules.Whisparr!.Count);
            Assert.Equal("Not an upgrade for existing episode", config.QueueCleanupRules.Whisparr[0].Match);
            Assert.Equal("remove", config.QueueCleanupRules.Whisparr[0].Action);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_QueueCleanupRules_EmptyLidarrList_Warns()
    {
        var originalError = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
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
                        ApiKey = "abc123",
                        ApiVersion = "1",
                        QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                    }
                },
                QueueCleanupRules = new QueueCleanupRulesConfig { Lidarr = new List<QueueCleanupRuleConfig>() }
            };
            var errors = YamlConfigLoader.Validate(config);

            Assert.Empty(errors);
            var warning = stderr.ToString();
            Assert.Contains("queueCleanupRules.lidarr", warning);
            Assert.Contains("empty", warning);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Validate_LidarrSearch_MissingSearchType_ReturnsError()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d" },
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "* * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("required"));
    }

    [Fact]
    public void Validate_LidarrSearch_ValidAlbumSearchType_Passes()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "album" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_LidarrSearch_ValidArtistSearchType_Passes()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "artist" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_LidarrSearch_SearchTypeCaseInsensitive_Passes()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "ALBUM" },
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "Artist" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhisparrSearch_InvalidSearchType_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "series" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("searchType") && e.Contains("episode") && e.Contains("season"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Lidarr_EmptyMatch_ReturnsError()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Lidarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("match") && e.Contains("empty"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Lidarr_InvalidAction_ReturnsError()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Lidarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "delete" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("action") && e.Contains("remove") && e.Contains("removeAndBlocklist"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Lidarr_EmptyAction_ReturnsError()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Lidarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("action") && e.Contains("required"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Lidarr_ErrorPrefixIncludesTypeAndIndex()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Lidarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Valid", Action = "remove" },
                    new() { Match = "", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        var error = errors.Single();
        Assert.Contains("queueCleanupRules.lidarr[1]", error);
    }

    [Fact]
    public void Validate_QueueCleanupRules_Whisparr_EmptyMatch_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Whisparr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("match") && e.Contains("empty"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Whisparr_InvalidAction_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Whisparr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "delete" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("action") && e.Contains("remove") && e.Contains("removeAndBlocklist"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Whisparr_EmptyAction_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Whisparr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Sample", Action = "" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("action") && e.Contains("required"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Whisparr_ErrorPrefixIncludesTypeAndIndex()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Whisparr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "Valid", Action = "remove" },
                    new() { Match = "", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        var error = errors.Single();
        Assert.Contains("queueCleanupRules.whisparr[1]", error);
    }

    [Fact]
    public void Validate_QueueCleanupRules_EmptyWhisparrList_Warns()
    {
        var originalError = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            var config = new AppConfig
            {
                Instances = new List<InstanceConfig>
                {
                    new()
                    {
                        Type = "whisparr",
                        Name = "Adult",
                        Url = "http://localhost:6969",
                        ApiKey = "abc123",
                        QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                    }
                },
                QueueCleanupRules = new QueueCleanupRulesConfig { Whisparr = new List<QueueCleanupRuleConfig>() }
            };
            var errors = YamlConfigLoader.Validate(config);

            Assert.Empty(errors);
            var warning = stderr.ToString();
            Assert.Contains("queueCleanupRules.whisparr", warning);
            Assert.Contains("empty", warning);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Validate_QueueCleanupRules_EmptyLidarrList_NoLidarrInstance_NoWarning()
    {
        var originalError = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
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
                },
                QueueCleanupRules = new QueueCleanupRulesConfig { Lidarr = new List<QueueCleanupRuleConfig>() }
            };
            var errors = YamlConfigLoader.Validate(config);

            Assert.Empty(errors);
            var output = stderr.ToString();
            Assert.DoesNotContain("queueCleanupRules.lidarr", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Validate_QueueCleanupRules_EmptyWhisparrList_NoWhisparrInstance_NoWarning()
    {
        var originalError = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
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
                },
                QueueCleanupRules = new QueueCleanupRulesConfig { Whisparr = new List<QueueCleanupRuleConfig>() }
            };
            var errors = YamlConfigLoader.Validate(config);

            Assert.Empty(errors);
            var output = stderr.ToString();
            Assert.DoesNotContain("queueCleanupRules.whisparr", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Validate_QueueCleanupRules_SonarrPresentLidarrAbsent_OnlySonarrWarns()
    {
        var originalError = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
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
                },
                QueueCleanupRules = new QueueCleanupRulesConfig
                {
                    Sonarr = new List<QueueCleanupRuleConfig>(),
                    Lidarr = new List<QueueCleanupRuleConfig>()
                }
            };
            var errors = YamlConfigLoader.Validate(config);

            Assert.Empty(errors);
            var output = stderr.ToString();
            Assert.Contains("queueCleanupRules.sonarr", output);
            Assert.DoesNotContain("queueCleanupRules.lidarr", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Validate_QueueCleanupRules_Lidarr_WhitespaceMatch_ReturnsError()
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
                    ApiKey = "abc123",
                    ApiVersion = "1",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Lidarr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "   ", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("match") && e.Contains("empty"));
    }

    [Fact]
    public void Validate_QueueCleanupRules_Whisparr_WhitespaceMatch_ReturnsError()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            },
            QueueCleanupRules = new QueueCleanupRulesConfig
            {
                Whisparr = new List<QueueCleanupRuleConfig>
                {
                    new() { Match = "   ", Action = "remove" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("match") && e.Contains("empty"));
    }

    [Fact]
    public void Validate_WhisparrSearch_SearchTypeCaseInsensitive_Passes()
    {
        var config = new AppConfig
        {
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "whisparr",
                    Name = "Adult",
                    Url = "http://localhost:6969",
                    ApiKey = "abc123",
                    MissingSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "SEASON" },
                    UpgradeSearch = new JobConfig { Enabled = true, Cron = "*/5 * * * *", MaxResults = 10, Cooldown = "30d", SearchType = "Episode" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("debug")]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("DEBUG")]
    [InlineData("Info")]
    public void Validate_ValidLogLevel_Passes(string level)
    {
        var config = new AppConfig
        {
            LogLevel = level,
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr", Name = "Series", Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InvalidLogLevel_ReturnsError()
    {
        var config = new AppConfig
        {
            LogLevel = "verbose",
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr", Name = "Series", Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Contains(errors, e => e.Contains("logLevel") && e.Contains("debug") && e.Contains("verbose"));
    }

    [Fact]
    public void Validate_NullLogLevel_Passes()
    {
        var config = new AppConfig
        {
            LogLevel = null,
            Instances = new List<InstanceConfig>
            {
                new()
                {
                    Type = "sonarr", Name = "Series", Url = "http://localhost:8989",
                    ApiKey = "abc123",
                    QueueCleanup = new JobConfig { Enabled = true, Cron = "*/5 * * * *" }
                }
            }
        };
        var errors = YamlConfigLoader.Validate(config);

        Assert.Empty(errors);
    }
}
