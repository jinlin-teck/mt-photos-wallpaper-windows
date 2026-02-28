using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MTPhotosWallpaper.Services;

public class WallpaperService
{
    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDWININICHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    public void SetWallpaper(string imagePath, string pictureOptions, string backgroundColor)
    {
        if (!File.Exists(imagePath))
            return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            if (key == null)
                return;

            var style = PictureOptionsToWindowsStyle(pictureOptions);
            key.SetValue("WallpaperStyle", style.WallpaperStyle);
            key.SetValue("TileWallpaper", style.TileWallpaper);

            if (!string.IsNullOrEmpty(backgroundColor))
            {
                key.SetValue("Background", backgroundColor);
            }

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
        catch
        {
        }
    }

    private (string WallpaperStyle, string TileWallpaper) PictureOptionsToWindowsStyle(string pictureOptions)
    {
        return pictureOptions switch
        {
            "centered" => ("0", "0"),
            "scaled" => ("6", "0"),
            "stretched" => ("2", "0"),
            "spanned" => ("22", "0"),
            "wallpaper" => ("0", "1"),
            _ => ("10", "0")
        };
    }
}
