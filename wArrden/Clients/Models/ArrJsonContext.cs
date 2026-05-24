using System.Text.Json.Serialization;

namespace wArrden.Clients.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WantedPagingResource<WantedEpisodeResource>))]
[JsonSerializable(typeof(WantedPagingResource<WantedMovieResource>))]
[JsonSerializable(typeof(WantedPagingResource<WantedAlbumResource>))]
[JsonSerializable(typeof(WantedPagingResource<QueueResource>))]
[JsonSerializable(typeof(IndexerResource[]))]
public partial class ArrJsonContext : JsonSerializerContext;
