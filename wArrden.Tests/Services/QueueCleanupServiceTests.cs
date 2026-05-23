using wArrden.Clients;
using wArrden.Clients.Models;
using wArrden.Services;

namespace wArrden.Tests;

public class QueueCleanupServiceTests
{
    private readonly Mock<IArrClient> _clientMock;
    private readonly OutputService _output;
    private readonly StringWriter _writer;

    public QueueCleanupServiceTests()
    {
        _clientMock = new Mock<IArrClient>();
        _clientMock.Setup(c => c.Instance).Returns("TestSonarr");
        _writer = new StringWriter();
        _output = new OutputService { Out = _writer };
    }

    private static List<QueueCleanupRule> SonarrRules() => new()
    {
        new("No files found are eligible", true),
        new("Not an upgrade for existing episode", false),
    };

    private static List<QueueCleanupRule> RadarrRules() => new()
    {
        new("Not an upgrade for existing movie", false),
        new("No files found are eligible", true),
    };

    private static List<QueueCleanupRule> LidarrRules() => new()
    {
        new("Not an upgrade for existing track file", false),
        new("Has unmatched tracks", true),
    };

    private static List<QueueCleanupRule> WhisparrRules() => new()
    {
        new("Not an upgrade for existing episode", false),
        new("No files found are eligible", true),
    };

    [Fact]
    public async Task CleanAsync_NoBlockedItems_ShowsNoBlockedMessage()
    {
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource>());

        var rules = SonarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(0, result);
        var output = _writer.ToString();
        Assert.Contains("No warning queue items detected", output);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_BlocklistRule_DeletesWithBlocklist()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Title = "Test Show"
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = SonarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanAsync_RemoveRule_DeletesWithoutBlocklist()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing episode",
            Title = "Test Show"
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = SonarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_DryRun_SkipsDelete()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Title = "Test Show"
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = SonarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "sonarr", true, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains("Would blocklist", _writer.ToString());
    }

    [Fact]
    public async Task CleanAsync_RadarrType_UsesRadarrRules()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing movie",
            Movie = new QueueMovie { Id = 10, Title = "Inception", Year = 2010 }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = RadarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "radarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanAsync_NoMatchingRule_ReturnsZero()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Some completely unknown error",
            Title = "Test Show"
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = SonarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(0, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_MixedMatches_OnlyActsOnMatches()
    {
        var matched = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Title = "Match"
        };
        var unmatched = new QueueResource
        {
            Id = 2,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Unknown error",
            Title = "NoMatch"
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { matched, unmatched });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = SonarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(2, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_LidarrType_UsesLidarrRules()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing track file",
            Artist = new QueueArtist { Id = 1, ArtistName = "The Beatles" },
            Album = new QueueAlbum { Id = 10, Title = "Abbey Road" }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = LidarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "lidarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanAsync_LidarrBlocklistRule_DeletesWithBlocklist()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Has unmatched tracks",
            Artist = new QueueArtist { Id = 2, ArtistName = "Pink Floyd" }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = LidarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "lidarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanAsync_WhisparrType_UsesWhisparrRules()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing episode",
            Episode = new QueueEpisode
            {
                Id = 42,
                Title = "Scene 1",
                SeasonNumber = 2,
                EpisodeNumber = 3,
                Series = new QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = WhisparrRules();
        var service = new QueueCleanupService(_clientMock.Object, "whisparr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanAsync_WhisparrBlocklistRule_DeletesWithBlocklist()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Episode = new QueueEpisode
            {
                Id = 99,
                Title = "Scene 2",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Series = new QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = WhisparrRules();
        var service = new QueueCleanupService(_clientMock.Object, "whisparr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanAsync_LidarrDryRun_SkipsDelete()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing track file",
            Artist = new QueueArtist { Id = 1, ArtistName = "The Beatles" }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = LidarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "lidarr", true, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Would remove", _writer.ToString());
    }

    [Fact]
    public async Task CleanAsync_LidarrNoMatchingRule_ReturnsZero()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Some completely unknown error",
            Artist = new QueueArtist { Id = 1, ArtistName = "The Beatles" }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = LidarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "lidarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(0, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_WhisparrDryRun_SkipsDelete()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Episode = new QueueEpisode
            {
                Id = 1,
                Title = "Scene 1",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Series = new QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = WhisparrRules();
        var service = new QueueCleanupService(_clientMock.Object, "whisparr", true, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Would blocklist", _writer.ToString());
    }

    [Fact]
    public async Task CleanAsync_WhisparrNoMatchingRule_ReturnsZero()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Some completely unknown error",
            Episode = new QueueEpisode
            {
                Id = 1,
                Title = "Scene 1",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Series = new QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = WhisparrRules();
        var service = new QueueCleanupService(_clientMock.Object, "whisparr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(0, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_RadarrBlocklistRule_DeletesWithBlocklist()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Movie = new QueueMovie { Id = 10, Title = "Inception", Year = 2010 }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = RadarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "radarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanAsync_RadarrDryRun_SkipsDelete()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing movie",
            Movie = new QueueMovie { Id = 10, Title = "Inception", Year = 2010 }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = RadarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "radarr", true, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Would remove", _writer.ToString());
    }

    [Fact]
    public async Task CleanAsync_RadarrNoMatchingRule_ReturnsZero()
    {
        var item = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Some completely unknown error",
            Movie = new QueueMovie { Id = 10, Title = "Inception", Year = 2010 }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item });

        var rules = RadarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "radarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(0, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_LidarrMixedMatches_OnlyActsOnMatches()
    {
        var matched = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Has unmatched tracks",
            Artist = new QueueArtist { Id = 1, ArtistName = "The Beatles" }
        };
        var unmatched = new QueueResource
        {
            Id = 2,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Unknown error",
            Artist = new QueueArtist { Id = 2, ArtistName = "Pink Floyd" }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { matched, unmatched });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = LidarrRules();
        var service = new QueueCleanupService(_clientMock.Object, "lidarr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(2, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_WhisparrMixedMatches_OnlyActsOnMatches()
    {
        var matched = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Episode = new QueueEpisode
            {
                Id = 1,
                Title = "Scene 1",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Series = new QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };
        var unmatched = new QueueResource
        {
            Id = 2,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Unknown error",
            Episode = new QueueEpisode
            {
                Id = 2,
                Title = "Scene 2",
                SeasonNumber = 1,
                EpisodeNumber = 2,
                Series = new QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { matched, unmatched });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rules = WhisparrRules();
        var service = new QueueCleanupService(_clientMock.Object, "whisparr", false, _output, rules);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(2, It.IsAny<CancellationToken>()), Times.Never);
    }
}
