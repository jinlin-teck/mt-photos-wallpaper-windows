using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using MTPhotosWallpaper.Models;

namespace MTPhotosWallpaper.Services;

public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MT-Photos Wallpaper");

    private static readonly string SettingsFilePath = Path.Combine(
        SettingsDirectory, "settings.json");

    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MTPhotosWallpaper";

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

        // 更新开机启动注册表
        SetStartup(settings.StartOnBoot);
    }

    public void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                // 获取当前可执行文件路径
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // 忽略注册表操作失败
        }
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(SettingsDirectory))
        {
            Directory.CreateDirectory(SettingsDirectory);
        }
    }
}
