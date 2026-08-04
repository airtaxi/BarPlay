using BarPlay.Helpers;
using BarPlay.Messages;
using BarPlay.Services;
using BarPlay.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BarPlay;

public partial class App : Application
{
    private MainWindow? _window;

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

    private async Task InitializeMainWindowAsync()
    {
        if (_window is not null) return;

        var window = new MainWindow();
        _window = window;
        window.TaskbarContentHost.TaskbarWindowRecreated += OnTaskbarContentHostTaskbarWindowChanged;
        window.TaskbarContentHost.TaskbarWindowDisappeared += OnTaskbarContentHostTaskbarWindowChanged;
        window.Closed += OnWindowClosed;

        await window.PrepareTaskbarContentAsync();
        window.Activate();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is Window window)
        {
            window.Closed -= OnWindowClosed;
        }
    }

    private async void OnPreferredMonitorChanged(object recipient, PreferredMonitorChangedMessage message) => await RecreateMainWindowAsync();

    private async void OnPlacementChanged(object recipient, PlacementChangedMessage message) => await RecreateMainWindowAsync();

    private async void OnWidthChanged(object recipient, WidthChangedMessage message) => await RecreateMainWindowAsync();

    private async Task RecreateMainWindowAsync()
    {
        var oldWindow = _window;

        oldWindow?.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowChanged;
        oldWindow?.TaskbarContentHost.TaskbarWindowDisappeared -= OnTaskbarContentHostTaskbarWindowChanged;
        oldWindow?.Closed -= OnWindowClosed;
        _window = null;

        await InitializeMainWindowAsync();

        if (WindowHelper.IsWindowAlive(oldWindow)) oldWindow?.Close();
    }

    // The old window can only be closed safely while it is still alive: when the taskbar disappears,
    // the hosted window is destroyed together with the taskbar, and closing it again triggers a
    // fail-fast inside Microsoft.UI.Xaml while the host restores the window state.
    private async void OnTaskbarContentHostTaskbarWindowChanged(object? sender, EventArgs e)
    {
        var oldWindow = _window;

        oldWindow?.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowChanged;
        oldWindow?.TaskbarContentHost.TaskbarWindowDisappeared -= OnTaskbarContentHostTaskbarWindowChanged;
        oldWindow?.Closed -= OnWindowClosed;
        _window = null;

        // Wait for the taskbar to be ready before recreating the window.
        await Task.Delay(1000);
        await InitializeMainWindowAsync();
        if (WindowHelper.IsWindowAlive(oldWindow)) oldWindow?.Close();
    }
}
