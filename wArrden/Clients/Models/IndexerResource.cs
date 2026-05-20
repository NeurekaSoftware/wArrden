using System.Text.Json.Serialization;

namespace wArrden.Clients.Models;

public class IndexerResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("enableAutomaticSearch")]
    public bool EnableAutomaticSearch { get; set; }
}
