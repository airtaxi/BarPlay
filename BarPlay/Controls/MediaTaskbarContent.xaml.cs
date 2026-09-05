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
    private const double CompactHeightThreshold = 48;
    private const double DefaultArtworkButtonSize = 36;
    private const double CompactArtworkButtonSize = 28;
    private const double DefaultTitleFontSize = 12;
    private const double CompactTitleFontSize = 11;
    private const double DefaultDescriptionFontSize = 10;
    private const double CompactDescriptionFontSize = 9;
    private const double DefaultTitleDescriptionSpacing = 8;
    private const double CompactTitleDescriptionSpacing = 6;

    private static readonly Thickness s_defaultButtonMargin = new(4);
    private static readonly Thickness s_compactButtonMargin = new(4, 0, 4, 0);

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

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not UserControl usageControl) return;

        var isCompact = usageControl.ActualHeight < CompactHeightThreshold;
        MediaInfoButton.Margin = isCompact ? s_compactButtonMargin : s_defaultButtonMargin;

        var artworkButtonSize = isCompact ? CompactArtworkButtonSize : DefaultArtworkButtonSize;
        TaskbarArtworkButton.Width = artworkButtonSize;
        TaskbarArtworkButton.Height = artworkButtonSize;
        TaskbarArtworkBorder.Width = artworkButtonSize;
        TaskbarArtworkBorder.Height = artworkButtonSize;

        TaskbarTitleTextBlock.FontSize = isCompact ? CompactTitleFontSize : DefaultTitleFontSize;
        TaskbarDescriptionTextBlock.FontSize = isCompact ? CompactDescriptionFontSize : DefaultDescriptionFontSize;
        TaskbarTextStackPanel.Spacing = isCompact ? CompactTitleDescriptionSpacing : DefaultTitleDescriptionSpacing;
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
