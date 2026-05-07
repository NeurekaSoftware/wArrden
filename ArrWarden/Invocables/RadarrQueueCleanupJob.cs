using ArrWarden.Configuration;
using ArrWarden.Services;
using Coravel.Invocable;

namespace ArrWarden.Invocables;

public class RadarrQueueCleanupJob : IInvocable
{
    private readonly QueueCleanupService _service;
    private readonly WardenOptions _options;

    public RadarrQueueCleanupJob(QueueCleanupService service, WardenOptions options)
    {
        _service = service;
        _options = options;
    }

    public Task Invoke()
    {
        if (!_options.HasRadarr)
            return Task.CompletedTask;

        return _service.CleanAsync(CancellationToken.None);
    }
}
