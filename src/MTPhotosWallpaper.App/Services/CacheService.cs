using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MTPhotosWallpaper.Models;

namespace MTPhotosWallpaper.Services;

public class CacheService
{
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MT-Photos Wallpaper", "Cache");

    public string GetCacheDirectory()
    {
        EnsureDirectoryExists();
        return CacheDirectory;
    }

    public string GetCacheFilePath(Photo photo)
    {
        EnsureDirectoryExists();
        var filename = $"{photo.Id}{photo.GetFileExtension()}";
        return Path.Combine(CacheDirectory, filename);
    }

    public bool IsCached(Photo photo)
    {
        var path = GetCacheFilePath(photo);
        return File.Exists(path);
    }

    public async Task CleanupCacheAsync(int maxFiles, long maxSizeMb)
    {
        EnsureDirectoryExists();

        var maxSizeBytes = maxSizeMb * 1024 * 1024;

        var files = Directory.GetFiles(CacheDirectory)
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.LastWriteTime)
            .ToList();

        var totalSize = files.Sum(f => f.Length);
        var deleteIndex = 0;

        while (deleteIndex < files.Count &&
               (files.Count - deleteIndex > maxFiles || totalSize > maxSizeBytes))
        {
            try
            {
                files[deleteIndex].Delete();
                totalSize -= files[deleteIndex].Length;
                deleteIndex++;
            }
            catch
            {
                deleteIndex++;
            }
        }
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
        }
    }
}
