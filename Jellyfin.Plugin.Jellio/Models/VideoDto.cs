using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Jellio.Models;

public class VideoDto
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("thumbnail")]
    public required string Thumbnail { get; set; }

    [JsonPropertyName("available")]
    public required bool Available { get; set; }

    [JsonPropertyName("episode")]
    public required int Episode { get; set; }

    [JsonPropertyName("season")]
    public required int Season { get; set; }

    [JsonPropertyName("overview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Overview { get; set; }

    [JsonPropertyName("released")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Released { get; set; }
}
