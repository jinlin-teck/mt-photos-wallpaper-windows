using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using MTPhotosWallpaper.Models;

namespace MTPhotosWallpaper.Views;

public class TrayIcon : IDisposable
{
    private NotifyIcon? _notifyIcon = null;
    private AppSettings _settings;
    private MainWindow _mainWindow;
    private List<Photo> _photos = new();
    private string? _accessToken;
    private string? _authCode;
    private System.Windows.Threading.DispatcherTimer? _timer;
    private bool _disposed;

    public TrayIcon(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _settings = App.SettingsService.LoadSettings();
        
        CreateNotifyIcon();
        LoadCredentials();
        InitializeTimer();
    }

    private void CreateNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "MT-Photos Wallpaper",
            Visible = _settings.ShowTrayIcon,
            Icon = SystemIcons.Application
        };

        var contextMenu = new ContextMenuStrip();
        
        var prevItem = new ToolStripMenuItem("Previous Wallpaper");
        prevItem.Click += (s, e) => PreviousWallpaper();
        contextMenu.Items.Add(prevItem);

        var nextItem = new ToolStripMenuItem("Next Wallpaper");
        nextItem.Click += (s, e) => NextWallpaper();
        contextMenu.Items.Add(nextItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var pauseItem = new ToolStripMenuItem(_settings.IsPaused ? "Resume Rotation" : "Pause Rotation");
        pauseItem.Click += (s, e) => TogglePause(pauseItem);
        contextMenu.Items.Add(pauseItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (s, e) => ShowSettings();
        contextMenu.Items.Add(settingsItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Exit();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowSettings();
    }

    private void LoadCredentials()
    {
        var (_, password) = App.CredentialService.GetCredentials();
        if (!string.IsNullOrEmpty(_settings.ServerUrl) && 
            !string.IsNullOrEmpty(_settings.Username) && 
            !string.IsNullOrEmpty(password))
        {
            Task.Run(async () =>
            {
                var result = await App.ApiService.AuthenticateAsync(
                    _settings.ServerUrl, _settings.Username, password);
                if (result.Success)
                {
                    _accessToken = result.AccessToken;
                    _authCode = result.AuthCode;
                    await LoadPhotosAsync();
                }
            });
        }
    }

    private async Task LoadPhotosAsync()
    {
        Log($"LoadPhotosAsync: AlbumId={_settings.AlbumId}, ServerUrl={_settings.ServerUrl}, Token={(_accessToken != null)}");
        if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_settings.AlbumId))
        {
            Log("LoadPhotosAsync: Skip - empty token or albumId");
            return;
        }

        _photos = await App.ApiService.GetPhotosAsync(
            _settings.ServerUrl, _settings.AlbumId, _accessToken, _settings.MinAspectRatio);
        Log($"LoadPhotosAsync: Loaded {_photos.Count} photos");
    }

    private static void Log(string message)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MTPhotosWallpaper", "debug.log");
            var dir = Path.GetDirectoryName(logPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.AppendAllText(logPath, $"[{DateTime.Now}] {message}\n");
        }
        catch { }
    }

    private void InitializeTimer()
    {
        if (!_settings.IsPaused && _photos.Count > 0)
        {
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_settings.ChangeInterval)
            };
            _timer.Tick += (s, e) => NextWallpaper();
            _timer.Start();
        }
    }

    private void PreviousWallpaper()
    {
        if (_photos.Count == 0)
            return;

        if (_settings.RandomPlayback && _settings.History.Count > 0)
        {
            if (_settings.HistoryIndex > 0)
            {
                _settings.HistoryIndex--;
                _settings.CurrentPhotoIndex = _settings.History[_settings.HistoryIndex];
            }
        }
        else
        {
            _settings.CurrentPhotoIndex = (_settings.CurrentPhotoIndex - 1 + _photos.Count) % _photos.Count;
            _settings.History.Clear();
            _settings.HistoryIndex = -1;
        }

        ApplyCurrentPhoto();
        App.SettingsService.SaveSettings(_settings);
    }

    private void NextWallpaper()
    {
        if (_photos.Count == 0)
            return;

        if (_settings.RandomPlayback)
        {
            if (_settings.HistoryIndex < _settings.History.Count - 1)
            {
                _settings.HistoryIndex++;
                _settings.CurrentPhotoIndex = _settings.History[_settings.HistoryIndex];
            }
            else
            {
                int newIndex;
                do
                {
                    newIndex = Random.Shared.Next(_photos.Count);
                } while (newIndex == _settings.CurrentPhotoIndex && _photos.Count > 1);

                _settings.CurrentPhotoIndex = newIndex;
                _settings.History.Add(newIndex);
                _settings.HistoryIndex = _settings.History.Count - 1;

                if (_settings.History.Count > 50)
                {
                    _settings.History.RemoveAt(0);
                    _settings.HistoryIndex--;
                }
            }
        }
        else
        {
            _settings.CurrentPhotoIndex = (_settings.CurrentPhotoIndex + 1) % _photos.Count;
            _settings.History.Clear();
            _settings.HistoryIndex = -1;
        }

        ApplyCurrentPhoto();
        App.SettingsService.SaveSettings(_settings);
    }

    private async void ApplyCurrentPhoto()
    {
        if (_photos.Count == 0)
            return;

        var photo = _photos[_settings.CurrentPhotoIndex];
        var cachePath = App.CacheService.GetCacheFilePath(photo);

        if (!App.CacheService.IsCached(photo))
        {
            if (string.IsNullOrEmpty(_authCode))
            {
                await ReAuthenticateAsync();
            }

            if (!string.IsNullOrEmpty(_authCode))
            {
                var downloadedPath = await App.ApiService.DownloadPhotoAsync(
                    _settings.ServerUrl, photo, _authCode, cachePath);
                
                if (downloadedPath == null)
                {
                    await ReAuthenticateAsync();
                    if (!string.IsNullOrEmpty(_authCode))
                    {
                        downloadedPath = await App.ApiService.DownloadPhotoAsync(
                            _settings.ServerUrl, photo, _authCode, cachePath);
                    }
                }
            }
        }

        if (File.Exists(cachePath))
        {
            App.WallpaperService.SetWallpaper(
                cachePath, _settings.PictureOptions, _settings.BackgroundColor);
            _notifyIcon!.Text = $"MT-Photos Wallpaper\n{photo.Id}";
        }

        await App.CacheService.CleanupCacheAsync(_settings.MaxCacheFiles, _settings.MaxCacheSizeMb);
    }

    private async Task ReAuthenticateAsync()
    {
        var (_, password) = App.CredentialService.GetCredentials();
        if (!string.IsNullOrEmpty(_settings.ServerUrl) && 
            !string.IsNullOrEmpty(_settings.Username) && 
            !string.IsNullOrEmpty(password))
        {
            var result = await App.ApiService.AuthenticateAsync(
                _settings.ServerUrl, _settings.Username, password);
            if (result.Success)
            {
                _accessToken = result.AccessToken;
                _authCode = result.AuthCode;
            }
        }
    }

    private void TogglePause(ToolStripMenuItem item)
    {
        _settings.IsPaused = !_settings.IsPaused;
        item.Text = _settings.IsPaused ? "Resume Rotation" : "Pause Rotation";
        
        if (_settings.IsPaused)
        {
            _timer?.Stop();
            _timer = null;
        }
        else
        {
            InitializeTimer();
        }

        App.SettingsService.SaveSettings(_settings);
    }

    private void ShowSettings()
    {
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void Exit()
    {
        _notifyIcon!.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }

    public async Task ReloadAsync()
    {
        _settings = App.SettingsService.LoadSettings();
        _accessToken = null;
        _authCode = null;
        _photos.Clear();

        var (_, password) = App.CredentialService.GetCredentials();
        if (!string.IsNullOrEmpty(_settings.ServerUrl) &&
            !string.IsNullOrEmpty(_settings.Username) &&
            !string.IsNullOrEmpty(password))
        {
            var result = await App.ApiService.AuthenticateAsync(
                _settings.ServerUrl, _settings.Username, password);
            if (result.Success)
            {
                _accessToken = result.AccessToken;
                _authCode = result.AuthCode;
                await LoadPhotosAsync();
            }
        }

        // 重启定时器
        _timer?.Stop();
        if (!_settings.IsPaused && _photos.Count > 0)
        {
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_settings.ChangeInterval)
            };
            _timer.Tick += (s, e) => NextWallpaper();
            _timer.Start();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _timer?.Stop();
        _notifyIcon?.Dispose();
        _disposed = true;
    }
}
