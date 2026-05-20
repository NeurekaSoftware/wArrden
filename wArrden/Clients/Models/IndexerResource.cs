using System.Text.Json.Serialization;

namespace wArrden.Clients.Models;

public class IndexerResource
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
}
