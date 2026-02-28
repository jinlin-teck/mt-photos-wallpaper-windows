using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MTPhotosWallpaper.Models;
using MTPhotosWallpaper.Resources;

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
        ApplyLocalization();
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

    private void ApplyLocalization()
    {
        Title = StringsManager.GetString("WindowTitle");
        
        // 连接设置
        ConnectionSettingsTextBlock.Text = StringsManager.GetString("ConnectionSettings");
        ServerUrlTextBlock.Text = StringsManager.GetString("ServerUrl");
        UsernameTextBlock.Text = StringsManager.GetString("Username");
        PasswordTextBlock.Text = StringsManager.GetString("Password");
        TestConnectionButton.Content = StringsManager.GetString("TestConnection");
        ConnectionStatusTextBlock.Text = StringsManager.GetString("NotTested");
        
        // 壁纸设置
        WallpaperSettingsTextBlock.Text = StringsManager.GetString("WallpaperSettings");
        ChangeIntervalTextBlock.Text = StringsManager.GetString("ChangeIntervalSeconds");
        PictureOptionsTextBlock.Text = StringsManager.GetString("PictureOptions");
        BackgroundColorTextBlock.Text = StringsManager.GetString("BackgroundColor");
        MinimumAspectRatioTextBlock.Text = StringsManager.GetString("MinimumAspectRatio");
        RandomPlaybackCheckBox.Content = StringsManager.GetString("RandomPlayback");
        
        // 更新图片选项下拉框
        PictureOptionsComboBox.Items.Clear();
        PictureOptionsComboBox.Items.Add(new ComboBoxItem { Content = StringsManager.GetString("Zoom"), Tag = "zoom" });
        PictureOptionsComboBox.Items.Add(new ComboBoxItem { Content = StringsManager.GetString("Centered"), Tag = "centered" });
        PictureOptionsComboBox.Items.Add(new ComboBoxItem { Content = StringsManager.GetString("Scaled"), Tag = "scaled" });
        PictureOptionsComboBox.Items.Add(new ComboBoxItem { Content = StringsManager.GetString("Stretched"), Tag = "stretched" });
        PictureOptionsComboBox.Items.Add(new ComboBoxItem { Content = StringsManager.GetString("Spanned"), Tag = "spanned" });
        PictureOptionsComboBox.Items.Add(new ComboBoxItem { Content = StringsManager.GetString("WallpaperMode"), Tag = "wallpaper" });
        
        // 缓存设置
        CacheSettingsTextBlock.Text = StringsManager.GetString("CacheSettings");
        MaxCacheFilesLabelTextBlock.Text = StringsManager.GetString("MaxCacheFiles");
        MaxCacheSizeLabelTextBlock.Text = StringsManager.GetString("MaxCacheSizeMb");
        // 相册选择
        AlbumSelectionTextBlock.Text = StringsManager.GetString("AlbumSelection");
        SelectAlbumTextBlock.Text = StringsManager.GetString("SelectAlbum");
        RefreshAlbumsButton.Content = StringsManager.GetString("RefreshAlbums");
        
        // 其他设置
        OtherSettingsTextBlock.Text = StringsManager.GetString("OtherSettings");
        ShowTrayIconCheckBox.Content = StringsManager.GetString("ShowTrayIcon");
        PausedCheckBox.Content = StringsManager.GetString("PauseRotation");
        StartOnBootCheckBox.Content = StringsManager.GetString("StartOnBoot");
        
        // 按钮
        SaveButton.Content = StringsManager.GetString("SaveSettings");
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

        var (username, password) = App.CredentialService.GetCredentials();
        if (!string.IsNullOrEmpty(username))
        {
            UsernameTextBox.Text = username;
        }
        if (!string.IsNullOrEmpty(password))
        {
            PasswordBox.Password = password;
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
        TestConnectionButton.Content = StringsManager.GetString("Testing");
        ConnectionStatusTextBlock.Text = StringsManager.GetString("TestingConnection");
        ConnectionStatusTextBlock.Foreground = Brushes.Gray;

        var serverUrl = ServerUrlTextBox.Text.Trim();
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        var result = await App.ApiService.AuthenticateAsync(serverUrl, username, password);

        if (result.Success)
        {
            _accessToken = result.AccessToken;
            _authCode = result.AuthCode;
            ConnectionStatusTextBlock.Text = StringsManager.GetString("ConnectionSuccessful");
            ConnectionStatusTextBlock.Foreground = Brushes.Green;
            
            App.CredentialService.StorePassword(username, password);
            await LoadAlbumsAsync();
        }
        else
        {
            ConnectionStatusTextBlock.Text = result.Error ?? StringsManager.GetString("ConnectionFailed");
            ConnectionStatusTextBlock.Foreground = Brushes.Red;
        }

        TestConnectionButton.IsEnabled = true;
        TestConnectionButton.Content = StringsManager.GetString("TestConnection");
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
        AlbumComboBox.Items.Add(StringsManager.GetString("PleaseSelectAlbum"));
        _albumIds = new List<string> { "" };

        var serverUrl = ServerUrlTextBox.Text.Trim();
        _albums = await App.ApiService.GetAlbumsAsync(serverUrl, _accessToken);

        foreach (var album in _albums.OrderBy(a => a.Name))
        {
            var photoCount = album.GetDisplayPhotoCount();
            var displayName = photoCount.HasValue 
                ? $"{album.Name} ({photoCount} {StringsManager.GetString("PhotosLabel")})" 
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

        MessageBox.Show(StringsManager.GetString("SettingsSaved"), StringsManager.GetString("Success"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void UpdateIntervalDisplay()
    {
        var seconds = (int)IntervalSlider.Value;
        var minutes = seconds / 60;
        var hours = minutes / 60;
        var secondsLabel = StringsManager.GetString("Seconds");
        
        if (hours > 0)
        {
            var hourLabel = hours > 1 ? StringsManager.GetString("Hours") : StringsManager.GetString("Hour");
            IntervalTextBlock.Text = $"{seconds} {secondsLabel} ({hours} {hourLabel})";
        }
        else if (minutes > 0)
        {
            var minuteLabel = minutes > 1 ? StringsManager.GetString("Minutes") : StringsManager.GetString("Minute");
            IntervalTextBlock.Text = $"{seconds} {secondsLabel} ({minutes} {minuteLabel})";
        }
        else
        {
            IntervalTextBlock.Text = $"{seconds} {secondsLabel}";
        }
    }

    private void UpdateAspectRatioDisplay()
    {
        AspectRatioTextBlock.Text = AspectRatioSlider.Value.ToString("F1");
    }

    private void UpdateMaxCacheFilesDisplay()
    {
        MaxCacheFilesTextBlock.Text = $"{(int)MaxCacheFilesSlider.Value} {StringsManager.GetString("Files")}";
    }

    private void UpdateMaxCacheSizeDisplay()
    {
        MaxCacheSizeTextBlock.Text = $"{(int)MaxCacheSizeSlider.Value} {StringsManager.GetString("MB")}";
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
        // 直接返回内部值，不依赖显示文本
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
