using BarPlay.Models;
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

    private void OnSettingsFlyoutOpened(object sender, object e)
    {
        RefreshPreferredMonitorMenu();
        RefreshPlacementMenu();
        RefreshWidthMenu();
    }

    private void RefreshPreferredMonitorMenu()
    {
        var availableIdentities = TaskbarMonitor.GetAvailableMonitorIdentities();
        ViewModel.RefreshMonitorIdentities(availableIdentities);

        PreferredMonitorMenuFlyoutSubItem.Items.Clear();
        foreach (var option in ViewModel.MonitorIdentities)
        {
            var radioItem = new ToggleMenuFlyoutItem
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
            var radioItem = new ToggleMenuFlyoutItem()
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
        if (sender is ToggleMenuFlyoutItem radioItem && radioItem.Tag is int identity)
        {
            ViewModel.SelectMonitorIdentityCommand.Execute(identity);
        }
    }

    private void RefreshPlacementMenu()
    {
        ViewModel.RefreshPlacements();

        PlacementMenuFlyoutSubItem.Items.Clear();
        foreach (var option in ViewModel.Placements)
        {
            var radioItem = new ToggleMenuFlyoutItem
            {
                Text = option.DisplayName,
                IsChecked = option.IsChecked,
                Tag = option.Placement
            };
            radioItem.Click += OnPlacementRadioItemClick;
            PlacementMenuFlyoutSubItem.Items.Add(radioItem);
        }
    }

    private void OnPlacementRadioItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem radioItem && radioItem.Tag is TaskbarContentPlacement placement)
        {
            ViewModel.SelectPlacementCommand.Execute(placement);
        }
    }

    private void RefreshWidthMenu()
    {
        ViewModel.RefreshWidths();

        WidthMenuFlyoutSubItem.Items.Clear();
        foreach (var option in ViewModel.Widths)
        {
            var radioItem = new ToggleMenuFlyoutItem
            {
                Text = option.DisplayName,
                IsChecked = option.IsChecked,
                Tag = option.Width
            };
            radioItem.Click += OnWidthRadioItemClick;
            WidthMenuFlyoutSubItem.Items.Add(radioItem);
        }
    }

    private void OnWidthRadioItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem radioItem && radioItem.Tag is TaskbarWidth width)
        {
            ViewModel.SelectWidthCommand.Execute(width);
        }
    }
}
