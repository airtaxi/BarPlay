using BarPlay.Services;
using BarPlay.ViewModels;
using CommunityToolkit.WinUI;
using Deskband11Lib.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace BarPlay.Controls;

public sealed partial class MediaTaskbarContent : UserControl
{
    public MediaPlaybackViewModel ViewModel { get; }

    private readonly ISettingsService _settingsService = App.Services.GetRequiredService<ISettingsService>();

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

    private void OnSeekSliderManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) => ViewModel.BeginSeek();
    private async void OnSeekSliderManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) => await ViewModel.EndSeekAsync((long)((Slider)sender).Value);
    private async void OnSeekSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e) => await ViewModel.SeekFromPositionChangeAsync(e.OldValue, e.NewValue);

    private void OnFlyoutSpaceKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ViewModel.TogglePlayPauseCommand.Execute(null);
    private void OnFlyoutOpened(object sender, object e)
    {
        if (ViewModel.FocusPlayPauseButtonOnFlyoutOpen)
        {
            FlyoutPlayPauseButton.Focus(FocusState.Keyboard);
        }
    }

    private void OnSettingsFlyoutOpened(object sender, object e) => RefreshPreferredMonitorMenu();

    private void RefreshPreferredMonitorMenu()
    {
        var availableIdentities = TaskbarMonitor.GetAvailableMonitorIdentities();
        ViewModel.RefreshMonitorIdentities(availableIdentities);

        PreferredMonitorMenuFlyoutSubItem.Items.Clear();
        foreach (var option in ViewModel.MonitorIdentities)
        {
            var radioItem = new RadioMenuFlyoutItem
            {
                Text = option.DisplayName,
                IsChecked = option.IsChecked,
                Tag = option.Identity
            };
            radioItem.Click += OnPreferredMonitorRadioItemClick;
            PreferredMonitorMenuFlyoutSubItem.Items.Add(radioItem);
        }

        var hasCurrentIdentity = ViewModel.MonitorIdentities.Any(x => x.Identity == _settingsService.PreferredMonitorIdentity);
        if (!hasCurrentIdentity)
        {
            var radioItem = new RadioMenuFlyoutItem()
            {
                Text = ViewModel.GetMonitorIdentityDisplayName(_settingsService.PreferredMonitorIdentity),
                IsChecked = true,
                IsEnabled = false
            };
            PreferredMonitorMenuFlyoutSubItem.Items.Add(radioItem);
        }
    }

    private void OnPreferredMonitorRadioItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem radioItem && radioItem.Tag is int identity)
        {
            ViewModel.SelectMonitorIdentityCommand.Execute(identity);
        }
    }
}
