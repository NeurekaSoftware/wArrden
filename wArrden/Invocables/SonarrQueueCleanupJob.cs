using wArrden.Clients;
using wArrden.Configuration;
using wArrden.Services;
using Coravel.Invocable;

namespace wArrden.Invocables;

public class SonarrQueueCleanupJob : IInvocable
{
    private readonly Func<IArrClient, string, QueueCleanupService> _factory;
    private readonly SonarrV3Client _client;
    private readonly WardenOptions _options;

    public SonarrQueueCleanupJob(Func<IArrClient, string, QueueCleanupService> factory, SonarrV3Client client, WardenOptions options)
    {
        _factory = factory;
        _client = client;
        _options = options;
    }

    public Task Invoke()
    {
        if (!_options.HasSonarr)
            return Task.CompletedTask;

        var service = _factory(_client, "SONARR");
        return service.CleanAsync(CancellationToken.None);
    }
}
