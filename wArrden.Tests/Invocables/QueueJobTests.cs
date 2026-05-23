using wArrden.Clients;
using wArrden.Invocables;
using wArrden.Services;

namespace wArrden.Tests;

public class QueueJobTests
{
    private readonly Mock<IArrClient> _clientMock;
    private readonly StringWriter _writer;
    private readonly OutputService _output;

    public QueueJobTests()
    {
        _clientMock = new Mock<IArrClient>();
        _clientMock.Setup(c => c.Instance).Returns("TestSonarr");
        _writer = new StringWriter();
        _output = new OutputService { Out = _writer };
    }

    [Fact]
    public async Task Invoke_EmptyQueue_ShowsNoBlockedMessage()
    {
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource>());

        var job = new QueueJob(_output, _clientMock.Object, "sonarr", false);

        await job.Invoke();

        _clientMock.Verify(c => c.GetQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains("No warning queue items detected", _writer.ToString());
    }

    [Fact]
    public async Task Invoke_DryRun_DoesNotDelete()
    {
        var item = new Clients.Models.QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing episode",
            Title = "Test"
        };

        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource> { item });

        var rules = new List<QueueCleanupRule>
        {
            new("Not an upgrade for existing episode", false)
        };
        var job = new QueueJob(_output, _clientMock.Object, "sonarr", true, rules);

        await job.Invoke();

        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains("Would remove", _writer.ToString());
    }

    [Fact]
    public async Task Invoke_NoRules_DoesNotDelete()
    {
        var item = new Clients.Models.QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing episode",
            Title = "Test"
        };

        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource> { item });

        var job = new QueueJob(_output, _clientMock.Object, "sonarr", true, null);

        await job.Invoke();

        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Invoke_RadarrEmptyQueue_ShowsNoBlockedMessage()
    {
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource>());

        var job = new QueueJob(_output, _clientMock.Object, "radarr", false);

        await job.Invoke();

        _clientMock.Verify(c => c.GetQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("No warning queue items detected", _writer.ToString());
    }

    [Fact]
    public async Task Invoke_LidarrEmptyQueue_ShowsNoBlockedMessage()
    {
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource>());

        var job = new QueueJob(_output, _clientMock.Object, "lidarr", false);

        await job.Invoke();

        _clientMock.Verify(c => c.GetQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("No warning queue items detected", _writer.ToString());
    }

    [Fact]
    public async Task Invoke_WhisparrEmptyQueue_ShowsNoBlockedMessage()
    {
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource>());

        var job = new QueueJob(_output, _clientMock.Object, "whisparr", false);

        await job.Invoke();

        _clientMock.Verify(c => c.GetQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("No warning queue items detected", _writer.ToString());
    }

    [Fact]
    public async Task Invoke_RadarrDryRun_DoesNotDelete()
    {
        var item = new Clients.Models.QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing movie",
            Movie = new Clients.Models.QueueMovie { Id = 10, Title = "Inception", Year = 2010 }
        };

        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource> { item });

        var rules = new List<QueueCleanupRule> { new("Not an upgrade for existing movie", false) };
        var job = new QueueJob(_output, _clientMock.Object, "radarr", true, rules);

        await job.Invoke();

        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Would remove", _writer.ToString());
    }

    [Fact]
    public async Task Invoke_LidarrDryRun_DoesNotDelete()
    {
        var item = new Clients.Models.QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing track file",
            Artist = new Clients.Models.QueueArtist { Id = 1, ArtistName = "The Beatles" }
        };

        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource> { item });

        var rules = new List<QueueCleanupRule> { new("Not an upgrade for existing track file", false) };
        var job = new QueueJob(_output, _clientMock.Object, "lidarr", true, rules);

        await job.Invoke();

        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Would remove", _writer.ToString());
    }

    [Fact]
    public async Task Invoke_WhisparrDryRun_DoesNotDelete()
    {
        var item = new Clients.Models.QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "Not an upgrade for existing episode",
            Episode = new Clients.Models.QueueEpisode
            {
                Id = 1,
                Title = "Scene 1",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Series = new Clients.Models.QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };

        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Clients.Models.QueueResource> { item });

        var rules = new List<QueueCleanupRule> { new("Not an upgrade for existing episode", false) };
        var job = new QueueJob(_output, _clientMock.Object, "whisparr", true, rules);

        await job.Invoke();

        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Would remove", _writer.ToString());
    }
}
