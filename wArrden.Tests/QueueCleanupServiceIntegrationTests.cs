using wArrden.Clients;
using wArrden.Clients.Models;
using wArrden.Services;

namespace wArrden.Tests;

public class QueueCleanupServiceIntegrationTests
{
    private readonly Mock<IArrClient> _clientMock;
    private readonly OutputService _output;
    private readonly StringWriter _writer;

    public QueueCleanupServiceIntegrationTests()
    {
        _clientMock = new Mock<IArrClient>();
        _clientMock.Setup(c => c.Instance).Returns("TestSonarr");
        _writer = new StringWriter();
        _output = new OutputService { Out = _writer };
    }

    [Fact]
    public async Task CleanAsync_NoBlockedItems_ReturnsZero()
    {
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource>());

        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(0, result);
        var output = _writer.ToString();
        Assert.Contains("No blocked queue items detected", output);
    }

    [Fact]
    public async Task CleanAsync_BlockedItems_DeletesWithBlocklist()
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

        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanAsync_BlockedItems_DeletesWithoutBlocklist()
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

        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output);
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

        var service = new QueueCleanupService(_clientMock.Object, "sonarr", true, _output);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains("Would blocklist", _writer.ToString());
    }

    [Fact]
    public async Task CleanAsync_UsesRadarrRulesForRadarrType()
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

        var service = new QueueCleanupService(_clientMock.Object, "radarr", false, _output);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemWithoutBlocklistAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanAsync_BlockedButNoMatch_ReturnsZero()
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

        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(0, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_SomeMatchSomeDont_OnlyMatchesActedUpon()
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

        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(1, result);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.DeleteQueueItemAsync(2, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanAsync_ResultsSortedByTitle()
    {
        var item1 = new QueueResource
        {
            Id = 1,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Title = "Zeta Show"
        };
        var item2 = new QueueResource
        {
            Id = 2,
            TrackedDownloadStatus = "warning",
            ErrorMessage = "No files found are eligible",
            Title = "Alpha Show"
        };
        _clientMock.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueResource> { item1, item2 });
        _clientMock.Setup(c => c.DeleteQueueItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new QueueCleanupService(_clientMock.Object, "sonarr", false, _output);
        var result = await service.CleanAsync(CancellationToken.None);

        Assert.Equal(2, result);
        var output = _writer.ToString();
        var alphaIndex = output.IndexOf("Alpha Show", StringComparison.Ordinal);
        var zetaIndex = output.IndexOf("Zeta Show", StringComparison.Ordinal);
        Assert.True(alphaIndex < zetaIndex);
    }
}
