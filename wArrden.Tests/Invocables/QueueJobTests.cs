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
        Assert.Contains("No blocked queue items detected", _writer.ToString());
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
        Assert.Contains("Would blocklist", _writer.ToString());
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
}
