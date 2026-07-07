using BarPlay.Services;
using Deskband11Lib.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using WinUIEx;

namespace BarPlay;

public sealed partial class MainWindow : Window
{
    private const uint WindowEndSessionMessage = 0x0016;

    public TaskbarContentHost TaskbarContentHost { get; }

    private readonly WindowSubclassProcedure _windowSubclassProcedure;

    private delegate nint WindowSubclassProcedure(nint windowHandle, uint message, nint wParam, nint lParam, nuint subclassIdentifier, nuint referenceData);

    [LibraryImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowSubclass(nint windowHandle, WindowSubclassProcedure procedure, nuint subclassIdentifier, nuint referenceData);

    [LibraryImport("comctl32.dll")]
    private static partial nint DefSubclassProc(nint windowHandle, uint message, nint wParam, nint lParam);

    public MainWindow()
    {
        InitializeComponent();

        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        TaskbarContentHost ??= new TaskbarContentHost(this, (FrameworkElement)Content, new() { PreferredWidth = 380, PreferredHeight = 48, PreferredMonitorIdentity = settingsService.PreferredMonitorIdentity });

        _windowSubclassProcedure = OnWindowSubclassProcedure;
        _ = SetWindowSubclass(this.GetWindowHandle(), _windowSubclassProcedure, 1, 0);
    }

    public async Task PrepareTaskbarContentAsync() => await TaskbarContentHost.AttachWhenLayoutReadyAsync();

    private void OnWindowClosed(object sender, WindowEventArgs e) => TaskbarContentHost?.Dispose();

    private nint OnWindowSubclassProcedure(nint windowHandle, uint message, nint wParam, nint lParam, nuint subclassIdentifier, nuint referenceData)
    {
        if (message == WindowEndSessionMessage && wParam != 0) Environment.Exit(0);

        return DefSubclassProc(windowHandle, message, wParam, lParam);
    }
}
