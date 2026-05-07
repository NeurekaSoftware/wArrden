using ArrWarden.Clients;
using ArrWarden.Configuration;
using ArrWarden.Services;
using Coravel.Invocable;

namespace ArrWarden.Invocables;

public class RadarrQueueCleanupJob : IInvocable
{
    private readonly RadarrV3Client _client;
    private readonly WardenOptions _options;
    private readonly OutputService _output;

    public RadarrQueueCleanupJob(RadarrV3Client client, WardenOptions options, OutputService output)
    {
        _client = client;
        _options = options;
        _output = output;
    }

    public Task Invoke()
    {
        if (!_options.HasRadarr)
            return Task.CompletedTask;

        var service = new QueueCleanupService(_client, _options, "RADARR", _output);
        return service.CleanAsync(CancellationToken.None);
    }
}
