using ArrWarden.Configuration;
using ArrWarden.Services;
using Coravel.Invocable;

namespace ArrWarden.Invocables;

public class SonarrQueueCleanupJob : IInvocable
{
    private readonly QueueCleanupService _service;
    private readonly WardenOptions _options;

    public SonarrQueueCleanupJob(QueueCleanupService service, WardenOptions options)
    {
        _service = service;
        _options = options;
    }

    public Task Invoke()
    {
        if (!_options.HasSonarr)
            return Task.CompletedTask;

        return _service.CleanAsync(CancellationToken.None);
    }
}
