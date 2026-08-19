using BarPlay.Messages;
using BarPlay.Models;
using BarPlay.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Deskband11Lib.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BarPlay.ViewModels;

public sealed partial class MediaPlaybackViewModel : ObservableObject, IDisposable
{
    private static readonly double s_immediateSeekThresholdTicks = TimeSpan.FromSeconds(2).Ticks;

    public IStartupTaskService StartupTaskService { get; }
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private readonly ISystemMediaTransportService _service;
    private bool _isUserSeeking;
    private bool _isApplyingSnapshotPosition;
    private bool _isDisposed;
    private bool _hasOptimisticToggle;

    public MediaPlaybackViewModel(ISystemMediaTransportService service, IStartupTaskService startupTaskService, ISettingsService settingsService, ILocalizationService localizationService)
    {
        _service = service;
        _settingsService = settingsService;
        _localizationService = localizationService;
        StartupTaskService = startupTaskService;

        _service.StateChanged += OnStateChanged;
    }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDescription))]
    public partial string Description { get; set; } = string.Empty;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    [ObservableProperty]
    public partial ImageSource? Thumbnail { get; set; }

    [ObservableProperty]
    public partial bool IsThumbnailVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoSession))]
    public partial bool HasSession { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotPlaying))]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool CanSkipPrevious { get; set; }

    [ObservableProperty]
    public partial bool CanSkipNext { get; set; }

    [ObservableProperty]
    public partial bool CanPlayPause { get; set; }

    [ObservableProperty]
    public partial bool HasTimeline { get; set; }

    public bool FocusPlayPauseButtonOnFlyoutOpen
    {
        get => _settingsService.FocusPlayPauseButtonOnFlyoutOpen;
        set
        {
            if (_settingsService.FocusPlayPauseButtonOnFlyoutOpen == value) return;
            _settingsService.FocusPlayPauseButtonOnFlyoutOpen = value;
            OnPropertyChanged(nameof(FocusPlayPauseButtonOnFlyoutOpen));
        }
    }

    [ObservableProperty]
    public partial IReadOnlyList<MonitorIdentityOption> MonitorIdentities { get; set; } = [];

    public void RefreshMonitorIdentities(IReadOnlyList<int> availableIdentities)
    {
        var currentIdentity = _settingsService.PreferredMonitorIdentity;
        MonitorIdentities = [.. availableIdentities.Select(identity => new MonitorIdentityOption { Identity = identity, DisplayName = GetMonitorIdentityDisplayName(identity), IsChecked = identity == currentIdentity })];
    }

    [RelayCommand]
    private void SelectMonitorIdentity(int identity)
    {
        if (_settingsService.PreferredMonitorIdentity == identity) return;
        _settingsService.PreferredMonitorIdentity = identity;
        RefreshMonitorIdentities([.. MonitorIdentities.Select(option => option.Identity)]);
        WeakReferenceMessenger.Default.Send<PreferredMonitorChangedMessage>();
    }

    public string GetMonitorIdentityDisplayName(int identity)
    {
        if (identity <= 0) return _localizationService.GetString("MonitorIdentityPrimary");
        return _localizationService.GetFormattedString("MonitorIdentitySecondary", identity);
    }

    [ObservableProperty]
    public partial List<TaskbarPlacementOption> Placements { get; set; } = [];

    public void RefreshPlacements()
    {
        var currentPlacement = _settingsService.Placement;
        Placements = [.. Enum.GetValues<TaskbarContentPlacement>().Select(placement => new TaskbarPlacementOption { Placement = placement, DisplayName = GetPlacementDisplayName(placement), IsChecked = placement == currentPlacement })];
    }

    public string GetPlacementDisplayName(TaskbarContentPlacement placement) => placement switch
    {
        TaskbarContentPlacement.Auto => _localizationService.GetString("PlacementAuto"),
        TaskbarContentPlacement.LeftEdge => _localizationService.GetString("PlacementLeftEdge"),
        TaskbarContentPlacement.BeforeNotificationArea => _localizationService.GetString("PlacementBeforeNotificationArea"),
        TaskbarContentPlacement.BeforeStartButton => _localizationService.GetString("PlacementBeforeStartButton"),
        _ => placement.ToString()
    };

    [RelayCommand]
    private void SelectPlacement(TaskbarContentPlacement placement)
    {
        if (_settingsService.Placement == placement) return;
        _settingsService.Placement = placement;
        RefreshPlacements();
        WeakReferenceMessenger.Default.Send<PlacementChangedMessage>();
    }

    [ObservableProperty]
    public partial List<TaskbarWidthOption> Widths { get; set; } = [];

    public void RefreshWidths()
    {
        var currentWidth = _settingsService.Width;
        Widths = [.. Enum.GetValues<TaskbarWidth>().Select(width => new TaskbarWidthOption { Width = width, DisplayName = GetWidthDisplayName(width), IsChecked = width == currentWidth })];
    }

    public string GetWidthDisplayName(TaskbarWidth width) => width switch
    {
        TaskbarWidth.Narrow => _localizationService.GetString("WidthNarrow"),
        TaskbarWidth.Normal => _localizationService.GetString("WidthNormal"),
        TaskbarWidth.Wide => _localizationService.GetString("WidthWide"),
        TaskbarWidth.FillRemainingSpace => _localizationService.GetString("WidthFillRemainingSpace"),
        _ => width.ToString()
    };

    [RelayCommand]
    private void SelectWidth(TaskbarWidth width)
    {
        if (_settingsService.Width == width) return;
        _settingsService.Width = width;
        RefreshWidths();
        WeakReferenceMessenger.Default.Send<WidthChangedMessage>();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText))]
    public partial double PositionTicks { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndTimeText))]
    public partial double EndTimeTicks { get; set; }

    public bool IsNotPlaying => !IsPlaying;

    public bool HasNoSession => !HasSession;

    public string PositionText => FormatTime(PositionTicks);

    public string EndTimeText => FormatTime(EndTimeTicks);

    public Task InitializeAsync() => _service.InitializeAsync();

    public void BeginSeek() => _isUserSeeking = true;

    public async Task EndSeekAsync(long positionTicks) => await SeekAsync(positionTicks);

    public async Task SeekFromPositionChangeAsync(double oldPositionTicks, double newPositionTicks)
    {
        if (_isApplyingSnapshotPosition) return;
        if (_isUserSeeking) return;
        if (!HasTimeline) return;
        if (!double.IsFinite(oldPositionTicks) || !double.IsFinite(newPositionTicks)) return;
        if (Math.Abs(newPositionTicks - oldPositionTicks) < s_immediateSeekThresholdTicks) return;

        await SeekAsync((long)newPositionTicks);
    }

    [RelayCommand]
    private async Task SkipPreviousAsync() => await _service.SkipPreviousAsync();

    [RelayCommand]
    private async Task SkipNextAsync() => await _service.SkipNextAsync();

    [RelayCommand]
    private async Task TogglePlayPauseAsync()
    {
        if (await _service.TogglePlayPauseAsync())
        {
            IsPlaying = !IsPlaying;
            _hasOptimisticToggle = true;
        }
    }

    [RelayCommand]
    private async Task OpenSourceAppAsync() => await _service.OpenSourceAppAsync();

    [RelayCommand]
    private async Task ToggleAutoStartAsync(ToggleMenuFlyoutItem item) => item.IsChecked = await StartupTaskService.SetEnabledAsync(item.IsChecked);

    [RelayCommand]
    private void Exit() => Environment.Exit(0);

    public void OnThumbnailImageOpened(object sender, RoutedEventArgs e) => IsThumbnailVisible = true;

    public void OnThumbnailImageFailed(object sender, ExceptionRoutedEventArgs e) => IsThumbnailVisible = false;

    private void OnStateChanged(MediaPlaybackSnapshot snapshot)
    {
        Title = snapshot.Title;
        Description = snapshot.Description;
        Thumbnail = snapshot.Thumbnail;
        IsThumbnailVisible = snapshot.Thumbnail is not null;
        System.Diagnostics.Debug.WriteLine($"IsThumbnailVisible {IsThumbnailVisible}");
        HasSession = snapshot.HasSession;
        if (!snapshot.HasSession) IsThumbnailVisible = false;

        if (_hasOptimisticToggle)
        {
            if (snapshot.IsPlaying == IsPlaying)
            {
                _hasOptimisticToggle = false;
            }
        }
        else IsPlaying = snapshot.IsPlaying;

        CanSkipPrevious = snapshot.CanSkipPrevious;
        CanSkipNext = snapshot.CanSkipNext;
        CanPlayPause = snapshot.CanPlayPause;
        HasTimeline = snapshot.HasTimeline;

        _isApplyingSnapshotPosition = true;
        try
        {
            EndTimeTicks = snapshot.EndTimeTicks;
            if (!_isUserSeeking) PositionTicks = snapshot.PositionTicks;
        }
        finally { _isApplyingSnapshotPosition = false; }
    }

    private async Task SeekAsync(long positionTicks)
    {
        _isUserSeeking = true;
        try { await _service.SeekAsync(positionTicks); }
        finally { _isUserSeeking = false; }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _service.StateChanged -= OnStateChanged;
        _service.Dispose();
    }

    public static string FormatTime(double ticks)
    {
        if (ticks <= 0) return "0:00";

        var span = TimeSpan.FromTicks((long)ticks);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}" : $"{(int)span.TotalMinutes}:{span.Seconds:D2}";
    }
}
