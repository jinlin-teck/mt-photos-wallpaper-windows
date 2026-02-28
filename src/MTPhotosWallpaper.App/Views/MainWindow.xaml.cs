using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MTPhotosWallpaper.Models;

namespace MTPhotosWallpaper.Views;

public partial class MainWindow : Window
{
    private AppSettings? _settings = null;
    private string? _accessToken;
    private string? _authCode;
    private List<Album> _albums = new();
    private List<string> _albumIds = new() { "" };

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
        WireUpEvents();
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 隐藏窗口而不是关闭，程序继续在托盘运行
        e.Cancel = true;
        Hide();
    }

    private void LoadSettings()
    {
        _settings = App.SettingsService.LoadSettings();
        
        ServerUrlTextBox.Text = _settings.ServerUrl;
        UsernameTextBox.Text = _settings.Username;
        IntervalSlider.Value = _settings.ChangeInterval;
        UpdateIntervalDisplay();
        PictureOptionsComboBox.SelectedIndex = GetPictureOptionsIndex(_settings.PictureOptions);
        AspectRatioSlider.Value = _settings.MinAspectRatio;
        UpdateAspectRatioDisplay();
        RandomPlaybackCheckBox.IsChecked = _settings.RandomPlayback;
        MaxCacheFilesSlider.Value = _settings.MaxCacheFiles;
        UpdateMaxCacheFilesDisplay();
        MaxCacheSizeSlider.Value = _settings.MaxCacheSizeMb;
        UpdateMaxCacheSizeDisplay();
        ShowTrayIconCheckBox.IsChecked = _settings.ShowTrayIcon;
        PausedCheckBox.IsChecked = _settings.IsPaused;
        StartOnBootCheckBox.IsChecked = _settings.StartOnBoot;

        BackgroundColorTextBox.Text = _settings.BackgroundColor;

        var (username, _) = App.CredentialService.GetCredentials();
        if (!string.IsNullOrEmpty(username))
        {
            UsernameTextBox.Text = username;
        }
    }

    private void WireUpEvents()
    {
        TestConnectionButton.Click += TestConnectionButton_Click;
        RefreshAlbumsButton.Click += RefreshAlbumsButton_Click;
        SaveButton.Click += SaveButton_Click;
        
        IntervalSlider.ValueChanged += (s, e) => UpdateIntervalDisplay();
        AspectRatioSlider.ValueChanged += (s, e) => UpdateAspectRatioDisplay();
        MaxCacheFilesSlider.ValueChanged += (s, e) => UpdateMaxCacheFilesDisplay();
        MaxCacheSizeSlider.ValueChanged += (s, e) => UpdateMaxCacheSizeDisplay();
    }

    private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        TestConnectionButton.IsEnabled = false;
        TestConnectionButton.Content = "Testing...";
        ConnectionStatusTextBlock.Text = "Testing connection...";
        ConnectionStatusTextBlock.Foreground = Brushes.Gray;

        var serverUrl = ServerUrlTextBox.Text.Trim();
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        var result = await App.ApiService.AuthenticateAsync(serverUrl, username, password);

        if (result.Success)
        {
            _accessToken = result.AccessToken;
            _authCode = result.AuthCode;
            ConnectionStatusTextBlock.Text = "Connection successful!";
            ConnectionStatusTextBlock.Foreground = Brushes.Green;
            
            App.CredentialService.StorePassword(username, password);
            await LoadAlbumsAsync();
        }
        else
        {
            ConnectionStatusTextBlock.Text = result.Error ?? "Connection failed";
            ConnectionStatusTextBlock.Foreground = Brushes.Red;
        }

        TestConnectionButton.IsEnabled = true;
        TestConnectionButton.Content = "Test Connection";
    }

    private async void RefreshAlbumsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshAlbumsButton.IsEnabled = false;
        AlbumComboBox.IsEnabled = false;

        if (string.IsNullOrEmpty(_accessToken))
        {
            await TestConnectionAsync();
        }
        else
        {
            await LoadAlbumsAsync();
        }

        RefreshAlbumsButton.IsEnabled = true;
        AlbumComboBox.IsEnabled = true;
    }

    private async Task TestConnectionAsync()
    {
        var serverUrl = ServerUrlTextBox.Text.Trim();
        var username = UsernameTextBox.Text.Trim();
        var (_, password) = App.CredentialService.GetCredentials();

        if (string.IsNullOrEmpty(password))
        {
            password = PasswordBox.Password;
        }

        var result = await App.ApiService.AuthenticateAsync(serverUrl, username, password);
        if (result.Success)
        {
            _accessToken = result.AccessToken;
            _authCode = result.AuthCode;
            await LoadAlbumsAsync();
        }
    }

    private async Task LoadAlbumsAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return;

        AlbumComboBox.Items.Clear();
        AlbumComboBox.Items.Add("Please select an album");
        _albumIds = new List<string> { "" };

        var serverUrl = ServerUrlTextBox.Text.Trim();
        _albums = await App.ApiService.GetAlbumsAsync(serverUrl, _accessToken);

        foreach (var album in _albums.OrderBy(a => a.Name))
        {
            var photoCount = album.GetDisplayPhotoCount();
            var displayName = photoCount.HasValue 
                ? $"{album.Name} ({photoCount} photos)" 
                : album.Name;
            AlbumComboBox.Items.Add(displayName);
            _albumIds.Add(album.Id.ToString());
        }

        var currentIndex = _albumIds.IndexOf(_settings!.AlbumId);
        AlbumComboBox.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings!.ServerUrl = ServerUrlTextBox.Text.Trim();
        _settings.Username = UsernameTextBox.Text.Trim();
        _settings.ChangeInterval = (int)IntervalSlider.Value;
        _settings.PictureOptions = GetPictureOptionsValue(PictureOptionsComboBox.SelectedIndex);
        _settings.MinAspectRatio = AspectRatioSlider.Value;
        _settings.RandomPlayback = RandomPlaybackCheckBox.IsChecked == true;
        _settings.MaxCacheFiles = (int)MaxCacheFilesSlider.Value;
        _settings.MaxCacheSizeMb = (int)MaxCacheSizeSlider.Value;
        _settings.ShowTrayIcon = ShowTrayIconCheckBox.IsChecked == true;
        _settings.IsPaused = PausedCheckBox.IsChecked == true;
        _settings.StartOnBoot = StartOnBootCheckBox.IsChecked == true;

        _settings.BackgroundColor = BackgroundColorTextBox.Text;

        if (AlbumComboBox.SelectedIndex > 0 && AlbumComboBox.SelectedIndex < _albumIds.Count)
        {
            _settings.AlbumId = _albumIds[AlbumComboBox.SelectedIndex];
        }

        if (!string.IsNullOrEmpty(PasswordBox.Password))
        {
            App.CredentialService.StorePassword(_settings.Username, PasswordBox.Password);
        }

        App.SettingsService.SaveSettings(_settings);

        // 通知托盘图标重新加载
        if (App.TrayIcon != null)
        {
            _ = App.TrayIcon.ReloadAsync();
        }

        MessageBox.Show("Settings saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void UpdateIntervalDisplay()
    {
        var seconds = (int)IntervalSlider.Value;
        var minutes = seconds / 60;
        var hours = minutes / 60;
        
        if (hours > 0)
        {
            IntervalTextBlock.Text = $"{seconds} seconds ({hours} hour{(hours > 1 ? "s" : "")}";
        }
        else if (minutes > 0)
        {
            IntervalTextBlock.Text = $"{seconds} seconds ({minutes} minute{(minutes > 1 ? "s" : "")}";
        }
        else
        {
            IntervalTextBlock.Text = $"{seconds} seconds";
        }
    }

    private void UpdateAspectRatioDisplay()
    {
        AspectRatioTextBlock.Text = AspectRatioSlider.Value.ToString("F1");
    }

    private void UpdateMaxCacheFilesDisplay()
    {
        MaxCacheFilesTextBlock.Text = $"{(int)MaxCacheFilesSlider.Value} files";
    }

    private void UpdateMaxCacheSizeDisplay()
    {
        MaxCacheSizeTextBlock.Text = $"{(int)MaxCacheSizeSlider.Value} MB";
    }

    private int GetPictureOptionsIndex(string value)
    {
        return value switch
        {
            "centered" => 1,
            "scaled" => 2,
            "stretched" => 3,
            "spanned" => 4,
            "wallpaper" => 5,
            _ => 0
        };
    }

    private string GetPictureOptionsValue(int index)
    {
        return index switch
        {
            1 => "centered",
            2 => "scaled",
            3 => "stretched",
            4 => "spanned",
            5 => "wallpaper",
            _ => "zoom"
        };
    }
}
