using wArrden.Clients;
using wArrden.Configuration;
using wArrden.Services;
using Coravel.Invocable;

namespace wArrden.Invocables;

public class RadarrQueueCleanupJob : IInvocable
{
    private readonly Func<IArrClient, string, QueueCleanupService> _factory;
    private readonly RadarrV3Client _client;
    private readonly WardenOptions _options;

    public RadarrQueueCleanupJob(Func<IArrClient, string, QueueCleanupService> factory, RadarrV3Client client, WardenOptions options)
    {
        _factory = factory;
        _client = client;
        _options = options;
    }

    public Task Invoke()
    {
        if (!_options.HasRadarr)
            return Task.CompletedTask;

        var service = _factory(_client, "RADARR");
        return service.CleanAsync(CancellationToken.None);
    }
}
