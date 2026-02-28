using System;
using System.IO;
using System.Text.Json;
using MTPhotosWallpaper.Models;

namespace MTPhotosWallpaper.Services;

public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MT-Photos Wallpaper");

    private static readonly string SettingsFilePath = Path.Combine(
        SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings LoadSettings()
    {
        EnsureDirectoryExists();

        if (!File.Exists(SettingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        EnsureDirectoryExists();

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(SettingsDirectory))
        {
            Directory.CreateDirectory(SettingsDirectory);
        }
    }
}
