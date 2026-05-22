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
    private readonly List<QueueCleanupRule>? _rules;

    public QueueJob(OutputService output, IArrClient client, string instanceType, bool isDryRun,
        List<QueueCleanupRule>? rules = null)
    {
        _output = output;
        _client = client;
        _instanceType = instanceType;
        _isDryRun = isDryRun;
        _rules = rules;
    }

    public async Task Invoke()
    {
        try
        {
            var service = new QueueCleanupService(_client, _instanceType, _isDryRun, _output, _rules);
            await service.CleanAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _output.WriteError($"{_client.Instance.ToLowerInvariant()}.queue", "Queue cleanup job failed", ex);
        }
    }
}
