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

        await InitializeMainWindowAsync();

        oldWindow?.Close();
    }

    private async Task ReinitializeMainWindowAsync()
    {
        if (_window is not null)
        {
            _window.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowChanged;
            _window.TaskbarContentHost.TaskbarWindowDisappeared -= OnTaskbarContentHostTaskbarWindowChanged;
            _window.Closed -= OnWindowClosed;
            _window.Close();
            _window = null;
        }

        await Task.Delay(500);
        await InitializeMainWindowAsync();
    }

    private async void OnTaskbarContentHostTaskbarWindowChanged(object? sender, EventArgs e)
    {
        var oldWindow = _window;

        oldWindow?.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowChanged;
        oldWindow?.TaskbarContentHost.TaskbarWindowDisappeared -= OnTaskbarContentHostTaskbarWindowChanged;
        oldWindow?.Closed -= OnWindowClosed;

        await Task.Delay(1000);
        await InitializeMainWindowAsync();

        oldWindow?.Close();
    }
}
