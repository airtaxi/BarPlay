using BarPlay.Services;
using BarPlay.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace BarPlay.Controls;

public sealed partial class MediaTaskbarContent : UserControl
{
    public MediaPlaybackViewModel ViewModel { get; }

    public MediaTaskbarContent()
    {
        InitializeComponent();

        ViewModel = App.Services.GetRequiredService<MediaPlaybackViewModel>();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        AutoStartToggleMenuFlyoutItem.IsChecked = await ViewModel.StartupTaskService.IsEnabledAsync();
    }

    private void OnSliderManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) => ViewModel.BeginSeek();
    private async void OnSliderManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) => await ViewModel.EndSeekAsync((long)((Slider)sender).Value);

    private void OnFlyoutSpaceKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ViewModel.TogglePlayPauseCommand.Execute(null);
    private void OnFlyoutOpened(object sender, object e) => FlyoutPlayPauseButton.Focus(FocusState.Keyboard);
}
