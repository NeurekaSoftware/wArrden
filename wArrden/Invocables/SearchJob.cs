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
    private readonly List<string>? _indexerNames;

    public SearchJob(SearchService search, IArrClient client,
        string searchKind, string instanceType, int maxResults, string cooldownStr, string searchType, bool isDryRun, List<string>? indexerNames)
    {
        _search = search;
        _client = client;
        _searchKind = searchKind;
        _instanceType = instanceType;
        _maxResults = maxResults;
        _cooldown = DurationParser.Parse(cooldownStr);
        _searchType = searchType;
        _isDryRun = isDryRun;
        _indexerNames = indexerNames;
    }

    public Task Invoke()
    {
        var ct = CancellationToken.None;

        return (_searchKind, _instanceType) switch
        {
            ("missing", "sonarr") => _search.SearchMissingEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerNames, ct),
            ("upgrade", "sonarr") => _search.SearchUpgradeEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerNames, ct),
            ("missing", "radarr") => _search.SearchMissingMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, _indexerNames, ct),
            ("upgrade", "radarr") => _search.SearchUpgradeMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, _indexerNames, ct),
            ("missing", "whisparr") => _search.SearchMissingEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerNames, ct),
            ("upgrade", "whisparr") => _search.SearchUpgradeEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerNames, ct),
            _ => Task.CompletedTask
        };
    }
}
