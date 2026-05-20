using wArrden.Clients;
using wArrden.Configuration;
using wArrden.Services;
using Coravel.Invocable;

namespace wArrden.Invocables;

public class SearchJob : IInvocable
{
    private readonly SearchService _search;
    private readonly IArrClient _client;
    private readonly string _searchKind;
    private readonly string _instanceType;
    private readonly int _maxResults;
    private readonly TimeSpan _cooldown;
    private readonly string _searchType;
    private readonly bool _isDryRun;

    public SearchJob(SearchService search, IArrClient client,
        string searchKind, string instanceType, int maxResults, string cooldownStr, string searchType, bool isDryRun)
    {
        _search = search;
        _client = client;
        _searchKind = searchKind;
        _instanceType = instanceType;
        _maxResults = maxResults;
        _cooldown = DurationParser.Parse(cooldownStr);
        _searchType = searchType;
        _isDryRun = isDryRun;
    }

    public Task Invoke()
    {
        var ct = CancellationToken.None;

        return (_searchKind, _instanceType) switch
        {
            ("missing", "sonarr") => _search.SearchMissingEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, ct),
            ("upgrade", "sonarr") => _search.SearchUpgradeEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, ct),
            ("missing", "radarr") => _search.SearchMissingMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, ct),
            ("upgrade", "radarr") => _search.SearchUpgradeMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, ct),
            _ => Task.CompletedTask
        };
    }
}
