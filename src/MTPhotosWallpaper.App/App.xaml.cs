using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MTPhotosWallpaper.Services;
using MTPhotosWallpaper.Views;

namespace MTPhotosWallpaper;

public partial class App : Application
{
    public static SettingsService SettingsService { get; private set; } = null!;
    public static CredentialService CredentialService { get; private set; } = null!;
    public static CacheService CacheService { get; private set; } = null!;
    public static MTPhotosApiService ApiService { get; private set; } = null!;
    public static WallpaperService WallpaperService { get; private set; } = null!;

    public static TrayIcon? TrayIcon { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常处理
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // 初始化服务
        SettingsService = new SettingsService();
        CredentialService = new CredentialService();
        CacheService = new CacheService();
        ApiService = new MTPhotosApiService();
        WallpaperService = new WallpaperService();

        // 加载设置
        var settings = SettingsService.LoadSettings();

        // 创建主窗口
        MainWindow = new MainWindow();

        // 如果是开机启动且设置了开机隐藏窗口，则启动时隐藏窗口
        if (settings.StartOnBoot)
        {
            MainWindow.WindowState = WindowState.Minimized;
            MainWindow.ShowInTaskbar = false;
            MainWindow.Hide();
        }
        else
        {
            MainWindow.Show();
        }

        // 创建托盘图标
        TrayIcon = new TrayIcon((MainWindow)MainWindow);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TrayIcon?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.Handled = true;
        MessageBox.Show($"Error: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.SetObserved();
    }

    private void LogException(Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MTPhotosWallpaper", "error.log");
            var dir = Path.GetDirectoryName(logPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.AppendAllText(logPath, $"[{DateTime.Now}] {ex}\n\n");
        }
        catch { }
    }
}
