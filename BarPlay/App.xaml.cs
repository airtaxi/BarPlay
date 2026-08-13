using BarPlay.Messages;
using BarPlay.Services;
using BarPlay.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Deskband11Lib.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BarPlay;

public partial class App : Application
{
    private static MainWindow? s_mainWindow;

    public static IServiceProvider Services { get; private set; } = null!;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices();
        WeakReferenceMessenger.Default.Register<PreferredMonitorChangedMessage>(this, OnPreferredMonitorChanged);
        WeakReferenceMessenger.Default.Register<PlacementChangedMessage>(this, OnPlacementChanged);
        WeakReferenceMessenger.Default.Register<WidthChangedMessage>(this, OnWidthChanged);
        await Task.Delay(1000);
        await InitializeMainWindowAsync();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISystemMediaTransportService>(serviceProvider => new SystemMediaTransportService(DispatcherQueue.GetForCurrentThread()!));
        services.AddSingleton<IStartupTaskService, StartupTaskService>();
        services.AddSingleton<MediaPlaybackViewModel>();

        return services.BuildServiceProvider();
    }

    private static async Task InitializeMainWindowAsync()
    {
        if (s_mainWindow is not null) return;

        var mainWindow = new MainWindow();
        s_mainWindow = mainWindow;
        mainWindow.TaskbarContentHost.TaskbarWindowRecreationRequired += OnTaskbarContentHostTaskbarWindowRecreationRequired;

        await mainWindow.PrepareTaskbarContentAsync();
        mainWindow.Activate();
    }

    private async void OnPreferredMonitorChanged(object recipient, PreferredMonitorChangedMessage message) => await RecreateMainWindowAsync();

    private async void OnPlacementChanged(object recipient, PlacementChangedMessage message) => await RecreateMainWindowAsync();

    private async void OnWidthChanged(object recipient, WidthChangedMessage message) => await RecreateMainWindowAsync();

    private static async Task RecreateMainWindowAsync()
    {
        var oldMainWindow = s_mainWindow;
        if (oldMainWindow is not null) ReleaseMainWindow(oldMainWindow);

        await InitializeMainWindowAsync();

        if (oldMainWindow?.IsWindowAlive() == true) oldMainWindow.Close();
    }

    // The hosted window is recreated when Explorer restarts, the monitor is disconnected, or the system DPI changes,
    // so the main window must be recreated to attach to the new taskbar.
    private static async void OnTaskbarContentHostTaskbarWindowRecreationRequired(object? sender, EventArgs e)
    {
        var oldMainWindow = s_mainWindow;
        if (oldMainWindow is not null) ReleaseMainWindow(oldMainWindow);

        // Wait for the taskbar to be ready before recreating the main window.
        await Task.Delay(1000);
        await InitializeMainWindowAsync();

        if (oldMainWindow?.IsWindowAlive() == true) oldMainWindow.Close();
    }

    private static void ReleaseMainWindow(MainWindow mainWindow)
    {
        mainWindow.TaskbarContentHost.Dispose();
        if (ReferenceEquals(s_mainWindow, mainWindow)) s_mainWindow = null;
    }
}
