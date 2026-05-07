using System.Text.Json.Serialization;

namespace ArrWarden.Clients.Models;

public class CommandResource
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("queued")]
    public DateTime Queued { get; set; }
}
