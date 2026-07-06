using BarPlay.Models;
using BarPlay.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BarPlay.ViewModels;

public sealed partial class MediaPlaybackViewModel : ObservableObject, IDisposable
{
    private static readonly double s_immediateSeekThresholdTicks = TimeSpan.FromSeconds(2).Ticks;

    public IStartupTaskService StartupTaskService { get; }
    private readonly ISettingsService _settingsService;
    private readonly ISystemMediaTransportService _service;
    private bool _isUserSeeking;
    private bool _isApplyingSnapshotPosition;
    private bool _isDisposed;
    private bool _hasOptimisticToggle;

    public MediaPlaybackViewModel(ISystemMediaTransportService service, IStartupTaskService startupTaskService, ISettingsService settingsService)
    {
        _service = service;
        _settingsService = settingsService;
        StartupTaskService = startupTaskService;

        _service.StateChanged += OnStateChanged;
    }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ImageSource? Thumbnail { get; set; }

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

    private void OnStateChanged(MediaPlaybackSnapshot snapshot)
    {
        Title = snapshot.Title;
        Description = snapshot.Description;
        Thumbnail = snapshot.Thumbnail;
        HasSession = snapshot.HasSession;

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
