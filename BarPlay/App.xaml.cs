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
        window.TaskbarContentHost.TaskbarWindowRecreated += OnTaskbarContentHostTaskbarWindowRecreated;
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

    private async void OnPreferredMonitorChanged(object recipient, PreferredMonitorChangedMessage message)
    {
        var oldWindow = _window;

        oldWindow?.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowRecreated;
        oldWindow?.Closed -= OnWindowClosed;

        await InitializeMainWindowAsync();

        oldWindow?.Close();
        oldWindow = null;
    }

    private async Task ReinitializeMainWindowAsync()
    {
        if (_window is not null)
        {
            _window.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowRecreated;
            _window.Closed -= OnWindowClosed;
            _window.Close();
            _window = null;
        }

        await Task.Delay(500);
        await InitializeMainWindowAsync();
    }

    private async void OnTaskbarContentHostTaskbarWindowRecreated(object? sender, EventArgs e)
    {
        _window?.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowRecreated;

        await Task.Delay(1000);
        await InitializeMainWindowAsync();
    }
}
