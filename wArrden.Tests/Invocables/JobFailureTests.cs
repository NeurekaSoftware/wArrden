using System.Net;
using System.Text.Json;
using wArrden.Clients;
using wArrden.Invocables;
using wArrden.Services;

namespace wArrden.Tests;

// Exercises the shared 3-way failure policy through QueueJob, which calls GetQueueAsync first:
//   auth failure  -> disable instance + one console warning, no Beacon (ERROR)
//   environmental -> console warning only, no Beacon, instance stays enabled
//   defect        -> console ERROR (captured to Beacon)
public class JobFailureTests
{
    private static (QueueJob Job, StringWriter Writer, InstanceHealthTracker Health) BuildJob(Exception toThrow)
    {
        var client = new Mock<IArrClient>();
        client.Setup(c => c.Instance).Returns("Music");
        client.Setup(c => c.GetQueueAsync(It.IsAny<CancellationToken>())).ThrowsAsync(toThrow);

        var writer = new StringWriter();
        var output = new OutputService { Out = writer };
        var health = new InstanceHealthTracker();
        var job = new QueueJob(output, health, new QueueJobParams(client.Object, "lidarr", false, null, "music"));
        return (job, writer, health);
    }

    [Fact]
    public async Task AuthFailure_DisablesInstance_WarnsOnce_NoError()
    {
        var (job, writer, health) = BuildJob(
            new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized));

        await job.Invoke();

        Assert.False(health.IsEnabled("music"));
        var log = writer.ToString();
        Assert.Contains("disabled after authentication failure", log);
        Assert.Contains("WARN", log);
        Assert.DoesNotContain("ERROR", log);
    }

    [Fact]
    public async Task EnvironmentalFailure_WarnsOnly_DoesNotDisable_NoError()
    {
        var (job, writer, health) = BuildJob(
            new HttpRequestException("bad gateway", null, HttpStatusCode.BadGateway));

        await job.Invoke();

        Assert.True(health.IsEnabled("music"));
        var log = writer.ToString();
        Assert.Contains("WARN", log);
        Assert.DoesNotContain("ERROR", log);
    }

    [Fact]
    public async Task Defect_LogsError_DoesNotDisable()
    {
        var (job, writer, health) = BuildJob(new JsonException("unexpected response shape"));

        await job.Invoke();

        Assert.True(health.IsEnabled("music"));
        Assert.Contains("ERROR", writer.ToString());
    }

    [Fact]
    public async Task AuthFailure_ReportedOnlyOnce_AcrossRepeatedRuns()
    {
        var (job, writer, _) = BuildJob(
            new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized));

        await job.Invoke();
        await job.Invoke(); // instance already disabled — must not log again

        var occurrences = writer.ToString().Split("disabled after authentication failure").Length - 1;
        Assert.Equal(1, occurrences);
    }
}
