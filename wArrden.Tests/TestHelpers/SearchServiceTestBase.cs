using wArrden.Clients;
using wArrden.Services;

namespace wArrden.Tests;

public abstract class SearchServiceTestBase
{
    protected readonly Mock<ICooldownService> CooldownMock;
    protected readonly Mock<OutputService> OutputMock;
    protected readonly Mock<IArrClient> ClientMock;
    protected readonly SearchService Service;
    protected static readonly TimeSpan DefaultCooldown = TimeSpan.FromDays(30);

    protected SearchServiceTestBase()
    {
        CooldownMock = new Mock<ICooldownService>();
        OutputMock = new Mock<OutputService> { CallBase = true };
        OutputMock.Object.Out = TextWriter.Null;
        OutputMock.Object.Error = TextWriter.Null;
        ClientMock = new Mock<IArrClient>();
        ClientMock.Setup(c => c.Instance).Returns("Sonarr");

        Service = new SearchService(CooldownMock.Object, OutputMock.Object);
    }

    protected void SetupOutputCallback()
    {
        OutputMock
            .Setup(o => o.RunSearchWithOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<Func<OutputService.SearchOutputWriter, Task>>()))
            .Callback<string, string, int, Func<OutputService.SearchOutputWriter, Task>>(
                (_, _, _, logic) => logic(new NullSearchOutputWriter()).Wait())
            .Returns(Task.CompletedTask);
    }

    protected void SetupCleanExpired(string instance = "Sonarr", string category = "Missing")
    {
        CooldownMock
            .Setup(c => c.CleanExpiredAsync(instance, category, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected void SetupCooldownIds(string instance = "Sonarr", string category = "Missing", params int[] ids)
    {
        CooldownMock
            .Setup(c => c.GetCooldownIdsAsync(instance, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>(ids));
    }

    protected void SetupCooldownIdsAny(params int[] ids)
    {
        CooldownMock
            .Setup(c => c.GetCooldownIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>(ids));
    }

    protected void SetupCleanExpiredAny()
    {
        CooldownMock
            .Setup(c => c.CleanExpiredAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected void SetupHasIndexers(bool hasIndexers = true)
    {
        ClientMock.Setup(c => c.HasAnyEnabledIndexerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasIndexers);
    }

    protected void SetupEpisodeTrigger()
    {
        ClientMock
            .Setup(c => c.TriggerEpisodeSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected void SetupMovieTrigger()
    {
        ClientMock
            .Setup(c => c.TriggerMoviesSearchAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected void SetupSeasonTrigger()
    {
        ClientMock
            .Setup(c => c.TriggerSeasonSearchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
