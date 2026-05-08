using System.Text.Json.Serialization;

namespace wArrden.Clients.Models;

public class QueueResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("sizeleft")]
    public long SizeLeft { get; set; }

    [JsonPropertyName("timeleft")]
    public string? TimeLeft { get; set; }

    [JsonPropertyName("estimatedCompletionTime")]
    public DateTimeOffset? EstimatedCompletionTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("trackedDownloadStatus")]
    public string? TrackedDownloadStatus { get; set; }

    [JsonPropertyName("trackedDownloadState")]
    public string? TrackedDownloadState { get; set; }

    [JsonPropertyName("statusMessages")]
    public List<QueueStatusMessage>? StatusMessages { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("downloadId")]
    public string? DownloadId { get; set; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("indexer")]
    public string? Indexer { get; set; }

    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; set; }

    [JsonPropertyName("episodeId")]
    public int? EpisodeId { get; set; }

    [JsonPropertyName("movieId")]
    public int? MovieId { get; set; }

    [JsonPropertyName("seriesId")]
    public int? SeriesId { get; set; }

    [JsonPropertyName("episode")]
    public QueueEpisode? Episode { get; set; }

    [JsonPropertyName("movie")]
    public QueueMovie? Movie { get; set; }
}

public class QueueStatusMessage
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("messages")]
    public List<string>? Messages { get; set; }
}

public class QueueEpisodeSeriesResource
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}

public class QueueEpisode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    [JsonPropertyName("series")]
    public QueueEpisodeSeriesResource? Series { get; set; }
}

public class QueueMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}
