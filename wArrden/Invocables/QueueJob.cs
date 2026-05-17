using wArrden.Clients;
using wArrden.Services;
using Coravel.Invocable;

namespace wArrden.Invocables;

public class QueueJob : IInvocable
{
    private readonly OutputService _output;
    private readonly IArrClient _client;
    private readonly string _instanceType;
    private readonly bool _isDryRun;

    public QueueJob(OutputService output, IArrClient client, string instanceType, bool isDryRun)
    {
        _output = output;
        _client = client;
        _instanceType = instanceType;
        _isDryRun = isDryRun;
    }

    public async Task Invoke()
    {
        var service = new QueueCleanupService(_client, _instanceType, _isDryRun, _output);
        await service.CleanAsync(CancellationToken.None);
    }
}
