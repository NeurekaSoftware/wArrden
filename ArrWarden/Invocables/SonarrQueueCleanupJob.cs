using ArrWarden.Clients;
using ArrWarden.Configuration;
using ArrWarden.Services;
using Coravel.Invocable;

namespace ArrWarden.Invocables;

public class SonarrQueueCleanupJob : IInvocable
{
    private readonly SonarrV3Client _client;
    private readonly WardenOptions _options;
    private readonly OutputService _output;

    public SonarrQueueCleanupJob(SonarrV3Client client, WardenOptions options, OutputService output)
    {
        _client = client;
        _options = options;
        _output = output;
    }

    public Task Invoke()
    {
        if (!_options.HasSonarr)
            return Task.CompletedTask;

        var service = new QueueCleanupService(_client, _options, "SONARR", _output);
        return service.CleanAsync(CancellationToken.None);
    }
}
