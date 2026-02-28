using System;
using System.Text.Json.Serialization;

namespace MTPhotosWallpaper.Models;

public class Photo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = "JPEG";

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("MD5")]
    public string MD5 { get; set; } = string.Empty;

    public string GetFileExtension()
    {
        return FileType.Equals("PNG", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
    }

    public double GetAspectRatio()
    {
        return Height == 0 ? 0 : (double)Width / Height;
    }
}
