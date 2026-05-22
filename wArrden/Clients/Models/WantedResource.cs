using System.Text.Json.Serialization;

namespace wArrden.Clients.Models;

public class WantedPagingResource<T>
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("records")]
    public List<T> Records { get; set; } = new();
}

public class WantedEpisodeResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    [JsonPropertyName("series")]
    public WantedEpisodeSeriesResource? Series { get; set; }

    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("airDateUtc")]
    public DateTime? AirDateUtc { get; set; }

    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }

    [JsonPropertyName("lastSearchTime")]
    public DateTime? LastSearchTime { get; set; }
}

public class WantedEpisodeSeriesResource
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}

public class WantedMovieResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }

    [JsonPropertyName("lastSearchTime")]
    public DateTime? LastSearchTime { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}

public class WantedAlbumResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("album")]
    public WantedAlbumRecord? Album { get; set; }

    [JsonPropertyName("artist")]
    public WantedAlbumArtistResource? Artist { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    [JsonPropertyName("lastSearchTime")]
    public DateTime? LastSearchTime { get; set; }
}

public class WantedAlbumRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("artistId")]
    public int ArtistId { get; set; }
}

public class WantedAlbumArtistResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("artistName")]
    public string? ArtistName { get; set; }
}
