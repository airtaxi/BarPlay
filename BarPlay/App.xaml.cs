using BarPlay.Services;
using BarPlay.ViewModels;
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
        await Task.Delay(1000);
        await InitializeMainWindowAsync();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILocalizationService, LocalizationService>();
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

        await window.PrepareTaskbarContentAsync();
        window.Activate();
    }

    private async void OnTaskbarContentHostTaskbarWindowRecreated(object? sender, EventArgs e)
    {
        _window?.TaskbarContentHost.TaskbarWindowRecreated -= OnTaskbarContentHostTaskbarWindowRecreated;

        await Task.Delay(1000);
        await InitializeMainWindowAsync();
    }
}
