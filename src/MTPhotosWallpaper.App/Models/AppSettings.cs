using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MTPhotosWallpaper.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private int _changeInterval = 1800;

    [ObservableProperty]
    private string _pictureOptions = "zoom";

    [ObservableProperty]
    private string _backgroundColor = "#000000";

    [ObservableProperty]
    private double _minAspectRatio = 0.0;

    [ObservableProperty]
    private bool _randomPlayback = true;

    [ObservableProperty]
    private string _albumId = string.Empty;

    [ObservableProperty]
    private int _maxCacheFiles = 50;

    [ObservableProperty]
    private int _maxCacheSizeMb = 500;

    [ObservableProperty]
    private bool _showTrayIcon = true;

    [ObservableProperty]
    private bool _isPaused = false;

    [ObservableProperty]
    private bool _startOnBoot = false;

    [ObservableProperty]
    private int _currentPhotoIndex = 0;

    [ObservableProperty]
    private List<int> _history = new();

    [ObservableProperty]
    private int _historyIndex = -1;
}
