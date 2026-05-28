using System.Text.Json.Serialization;

namespace wArrden.Clients.Models;

public class TagResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }
}
