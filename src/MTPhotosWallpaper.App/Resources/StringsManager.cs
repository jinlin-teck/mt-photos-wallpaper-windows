using System;
using System.Globalization;
using System.Resources;

namespace MTPhotosWallpaper.Resources;

public static class StringsManager
{
    private static readonly ResourceManager _resourceManager = new ResourceManager(
        "MTPhotosWallpaper.Resources.Strings",
        typeof(StringsManager).Assembly);

    public static string GetString(string name)
    {
        try
        {
            return _resourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
        }
        catch
        {
            return name;
        }
    }

    public static string GetString(string name, CultureInfo culture)
    {
        try
        {
            return _resourceManager.GetString(name, culture) ?? name;
        }
        catch
        {
            return name;
        }
    }

    public static void SetCulture(string cultureName)
    {
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
        }
        catch
        {
            // 如果设置失败，使用默认文化
        }
    }
}
