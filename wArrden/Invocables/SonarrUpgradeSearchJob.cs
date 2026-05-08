using wArrden.Clients;
using wArrden.Configuration;
using wArrden.Services;
using Coravel.Invocable;

namespace wArrden.Invocables;

public class SonarrUpgradeSearchJob : IInvocable
{
    private readonly SearchService _service;
    private readonly SonarrV3Client _client;
    private readonly WardenOptions _options;

    public SonarrUpgradeSearchJob(SearchService service, SonarrV3Client client, WardenOptions options)
    {
        _service = service;
        _client = client;
        _options = options;
    }

    public Task Invoke()
    {
        if (!_options.HasSonarr)
            return Task.CompletedTask;

        return _service.SearchUpgradeEpisodesAsync(_client, CancellationToken.None);
    }
}
