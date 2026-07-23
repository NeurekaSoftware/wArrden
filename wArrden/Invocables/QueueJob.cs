using wArrden.Clients;
using wArrden.Services;
using Coravel.Invocable;

namespace wArrden.Invocables;

public sealed record QueueJobParams(
    IArrClient Client,
    string InstanceType,
    bool IsDryRun,
    List<QueueCleanupRule>? Rules = null,
    string InstanceKey = ""
);

public class QueueJob : IInvocable
{
    private readonly OutputService _output;
    private readonly InstanceHealthTracker _health;
    private readonly IArrClient _client;
    private readonly string _instanceType;
    private readonly string _instanceKey;
    private readonly bool _isDryRun;
    private readonly List<QueueCleanupRule>? _rules;

    public QueueJob(OutputService output, InstanceHealthTracker health, QueueJobParams p)
    {
        _output = output;
        _health = health;
        _client = p.Client;
        _instanceType = p.InstanceType;
        _instanceKey = p.InstanceKey;
        _isDryRun = p.IsDryRun;
        _rules = p.Rules;
    }

    public async Task Invoke()
    {
        var context = $"{_client.Instance.ToLowerInvariant()}.queue";
        try
        {
            var service = new QueueCleanupService(_client, _instanceType, _isDryRun, _output, _rules);
            await service.CleanAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            JobFailure.Report(_output, _health, _instanceKey, _client.Instance, context,
                "Queue cleanup job failed", ex);
        }
    }
}
