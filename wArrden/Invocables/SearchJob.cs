using wArrden.Clients;
using wArrden.Configuration;
using wArrden.Services;
using Coravel.Invocable;

namespace wArrden.Invocables;

public sealed record SearchJobParams(
    IArrClient Client,
    string SearchKind,
    string InstanceType,
    int MaxResults,
    string Cooldown,
    string SearchType,
    bool IsDryRun,
    IndexerFilterConfig? IndexerFilter,
    TaggingConfig? Tagging,
    string InstanceKey = ""
);

public class SearchJob : IInvocable
{
    private readonly SearchService _search;
    private readonly OutputService _output;
    private readonly InstanceHealthTracker _health;
    private readonly IArrClient _client;
    private readonly string _searchKind;
    private readonly string _instanceType;
    private readonly string _instanceKey;
    private readonly int _maxResults;
    private readonly TimeSpan _cooldown;
    private readonly string _searchType;
    private readonly bool _isDryRun;
    private readonly IndexerFilterConfig? _indexerFilter;
    private readonly TaggingConfig? _tagging;

    public SearchJob(SearchService search, OutputService output, InstanceHealthTracker health, SearchJobParams p)
    {
        _search = search;
        _output = output;
        _health = health;
        _client = p.Client;
        _searchKind = p.SearchKind;
        _instanceType = p.InstanceType;
        _instanceKey = p.InstanceKey;
        _maxResults = p.MaxResults;
        _cooldown = DurationParser.Parse(p.Cooldown);
        _searchType = p.SearchType;
        _isDryRun = p.IsDryRun;
        _indexerFilter = p.IndexerFilter;
        _tagging = p.Tagging;
    }

    public async Task Invoke()
    {
        var ct = CancellationToken.None;
        var jobKey = _searchKind == "missing" ? "missing" : "upgrade";
        var context = $"{_client.Instance.ToLowerInvariant()}.{jobKey}";

        try
        {
            var task = (_searchKind, _instanceType) switch
            {
                ("missing", "sonarr") => _search.SearchMissingEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerFilter, _tagging, ct),
                ("upgrade", "sonarr") => _search.SearchUpgradeEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerFilter, _tagging, ct),
                ("missing", "radarr") => _search.SearchMissingMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, _indexerFilter, _tagging, ct),
                ("upgrade", "radarr") => _search.SearchUpgradeMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, _indexerFilter, _tagging, ct),
                ("missing", "whisparr") => _search.SearchMissingEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerFilter, _tagging, ct),
                ("upgrade", "whisparr") => _search.SearchUpgradeEpisodesAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerFilter, _tagging, ct),
                ("missing", "whisparr-eros") => _search.SearchMissingMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, _indexerFilter, _tagging, ct),
                ("upgrade", "whisparr-eros") => _search.SearchUpgradeMoviesAsync(_client, _maxResults, _cooldown, _isDryRun, _indexerFilter, _tagging, ct),
                ("missing", "lidarr") => _search.SearchMissingAlbumsAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerFilter, _tagging, ct),
                ("upgrade", "lidarr") => _search.SearchUpgradeAlbumsAsync(_client, _maxResults, _cooldown, _searchType, _isDryRun, _indexerFilter, _tagging, ct),
                _ => throw new InvalidOperationException(
                    $"No search handler for kind='{_searchKind}' type='{_instanceType}'")
            };
            await task;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            JobFailure.Report(_output, _health, _instanceKey, _client.Instance, context,
                $"{_searchKind} search job failed", ex);
        }
    }
}
