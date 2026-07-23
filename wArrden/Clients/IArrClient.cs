using System.Net;
using wArrden.Clients.Models;

namespace wArrden.Clients;

public interface IArrClient : IDisposable
{
    string Instance { get; }

    Task<IReadOnlyList<QueueResource>> GetQueueAsync(CancellationToken ct);
    Task DeleteQueueItemAsync(int queueId, CancellationToken ct);
    Task DeleteQueueItemWithoutBlocklistAsync(int queueId, CancellationToken ct);
    Task<IReadOnlyList<WantedEpisodeResource>> GetWantedMissingEpisodesAsync(CancellationToken ct);
    Task<IReadOnlyList<WantedEpisodeResource>> GetWantedCutoffEpisodesAsync(CancellationToken ct);
    Task<IReadOnlyList<WantedMovieResource>> GetWantedMissingMoviesAsync(CancellationToken ct);
    Task<IReadOnlyList<WantedMovieResource>> GetWantedCutoffMoviesAsync(CancellationToken ct);
    Task TriggerEpisodeSearchAsync(int[] episodeIds, CancellationToken ct);
    Task TriggerSeasonSearchAsync(int seriesId, int seasonNumber, CancellationToken ct);
    Task TriggerMoviesSearchAsync(int[] movieIds, CancellationToken ct);
    Task<IReadOnlyList<WantedAlbumResource>> GetWantedMissingAlbumsAsync(CancellationToken ct);
    Task<IReadOnlyList<WantedAlbumResource>> GetWantedCutoffAlbumsAsync(CancellationToken ct);
    Task TriggerAlbumSearchAsync(int[] albumIds, CancellationToken ct);
    Task TriggerArtistSearchAsync(int artistId, CancellationToken ct);
    Task<bool> HasAnyEnabledIndexerAsync(CancellationToken ct);
    Task<IReadOnlyList<IndexerResource>> GetIndexersAsync(CancellationToken ct);
    Task<HttpStatusCode> ValidateApiKeyAsync(CancellationToken ct);

    Task<IReadOnlyList<TagResource>> GetTagsAsync(CancellationToken ct);
    Task<TagResource> CreateTagAsync(string label, CancellationToken ct);
    Task<bool> EnsureTagOnSeriesAsync(int seriesId, int tagId, CancellationToken ct);
    Task<bool> EnsureTagOnMovieAsync(int movieId, int tagId, CancellationToken ct);
    Task<bool> EnsureTagOnArtistAsync(int artistId, int tagId, CancellationToken ct);
    Task<HashSet<int>> ResolveSeriesIdsAsync(int[] episodeIds, CancellationToken ct);
    Task<HashSet<int>> ResolveArtistIdsAsync(int[] albumIds, CancellationToken ct);
}
