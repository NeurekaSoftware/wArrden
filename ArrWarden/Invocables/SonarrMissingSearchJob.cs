using ArrWarden.Clients;
using ArrWarden.Configuration;
using ArrWarden.Services;
using Coravel.Invocable;

namespace ArrWarden.Invocables;

public class SonarrMissingSearchJob : IInvocable
{
    private readonly SearchService _service;
    private readonly SonarrV3Client _client;
    private readonly WardenOptions _options;

    public SonarrMissingSearchJob(SearchService service, SonarrV3Client client, WardenOptions options)
    {
        _service = service;
        _client = client;
        _options = options;
    }

    public Task Invoke()
    {
        if (!_options.HasSonarr)
            return Task.CompletedTask;

        return _service.SearchMissingEpisodesAsync(_client, CancellationToken.None);
    }
}
