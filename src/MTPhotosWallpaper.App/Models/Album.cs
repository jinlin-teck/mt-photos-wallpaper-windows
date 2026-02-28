using System.Text.Json.Serialization;

namespace MTPhotosWallpaper.Models;

public class Album
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int? PhotoCount { get; set; }

    [JsonPropertyName("assetCount")]
    public int? AssetCount { get; set; }

    [JsonPropertyName("photoCount")]
    public int? PhotoCountAlt { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    public int? GetDisplayPhotoCount()
    {
        return PhotoCount ?? AssetCount ?? PhotoCountAlt ?? Total;
    }
}
