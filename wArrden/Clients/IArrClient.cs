using wArrden.Clients.Models;

namespace wArrden.Clients;

public interface IArrClient
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
}
