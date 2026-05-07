using ArrWarden.Clients;
using ArrWarden.Configuration;
using ArrWarden.Services;
using Coravel.Invocable;

namespace ArrWarden.Invocables;

public class RadarrUpgradeSearchJob : IInvocable
{
    private readonly SearchService _service;
    private readonly RadarrV3Client _client;
    private readonly WardenOptions _options;

    public RadarrUpgradeSearchJob(SearchService service, RadarrV3Client client, WardenOptions options)
    {
        _service = service;
        _client = client;
        _options = options;
    }

    public Task Invoke()
    {
        if (!_options.HasRadarr)
            return Task.CompletedTask;

        return _service.SearchUpgradeMoviesAsync(_client, CancellationToken.None);
    }
}
