using BarPlay.Models;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace BarPlay.Services;


public sealed partial class SystemMediaTransportService : ISystemMediaTransportService
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherTimer _positionTimer;
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private BitmapImage? _cachedThumbnail;
    private bool _isDisposed;

    public SystemMediaTransportService(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _positionTimer.Tick += OnPositionTimerTick;
    }

    public event Action<MediaPlaybackSnapshot>? StateChanged;

    public async Task InitializeAsync()
    {
        _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _sessionManager.SessionsChanged += OnSessionsChanged;
        _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

        UpdateCurrentSession();
        await RefreshSnapshotAsync();

        _positionTimer.Start();
    }

    public async Task<bool> SkipPreviousAsync()
    {
        if (_currentSession is null) return false;
        return await _currentSession.TrySkipPreviousAsync();
    }

    public async Task<bool> SkipNextAsync()
    {
        if (_currentSession is null) return false;
        return await _currentSession.TrySkipNextAsync();
    }

    public async Task<bool> TogglePlayPauseAsync()
    {
        if (_currentSession is null) return false;
        return await _currentSession.TryTogglePlayPauseAsync();
    }

    public async Task<bool> SeekAsync(long positionTicks)
    {
        if (_currentSession is null) return false;
        return await _currentSession.TryChangePlaybackPositionAsync(positionTicks);
    }

    public Task<bool> OpenSourceAppAsync()
    {
        var sourceAppUserModelId = _currentSession?.SourceAppUserModelId;
        if (string.IsNullOrWhiteSpace(sourceAppUserModelId)) return Task.FromResult(false);

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = $@"shell:AppsFolder\{sourceAppUserModelId}",
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
            return Task.FromResult(true);
        }
        catch { return Task.FromResult(false); }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _positionTimer.Stop();
        _positionTimer.Tick -= OnPositionTimerTick;

        DetachCurrentSession();

        if (_sessionManager is not null)
        {
            _sessionManager.SessionsChanged -= OnSessionsChanged;
            _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) => _dispatcherQueue.TryEnqueue(() => _ = RefreshSnapshotAsync());

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) => _dispatcherQueue.TryEnqueue(OnCurrentSessionChangedOnDispatcher);

    private void OnCurrentSessionChangedOnDispatcher()
    {
        UpdateCurrentSession();
        _ = RefreshSnapshotAsync();
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _cachedThumbnail = null;
        _dispatcherQueue.TryEnqueue(() => _ = RefreshSnapshotAsync());
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) => _dispatcherQueue.TryEnqueue(() => _ = RefreshSnapshotAsync());

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) => _dispatcherQueue.TryEnqueue(() => _ = RefreshSnapshotAsync());

    private void OnPositionTimerTick(object? sender, object e) => _ = RefreshSnapshotAsync();

    private void UpdateCurrentSession()
    {
        DetachCurrentSession();

        _cachedThumbnail = null;
        _currentSession = _sessionManager?.GetCurrentSession();

        if (_currentSession is not null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        }
    }

    private void DetachCurrentSession()
    {
        if (_currentSession is null) return;

        _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _currentSession = null;
    }

    private async Task RefreshSnapshotAsync()
    {
        if (_isDisposed) return;
        if (_sessionManager is null) return;

        var session = _currentSession;
        if (session is null)
        {
            RaiseStateChanged(new MediaPlaybackSnapshot(string.Empty, string.Empty, false, false, false, false, false, null, 0, 0, false));
            return;
        }

        var mediaProperties = await session.TryGetMediaPropertiesAsync();
        if (_isDisposed) return;

        var playbackInfo = session.GetPlaybackInfo();
        var timeline = session.GetTimelineProperties();

        if (_cachedThumbnail is null && mediaProperties.Thumbnail is not null) _cachedThumbnail = await LoadThumbnailAsync(mediaProperties.Thumbnail);

        var isPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        var effectivePosition = CalculateEffectivePosition(timeline, playbackInfo);
        var hasTimeline = timeline.EndTime > TimeSpan.Zero;
        var controls = playbackInfo.Controls;

        var snapshot = new MediaPlaybackSnapshot(mediaProperties.Title ?? string.Empty, BuildDescription(mediaProperties.Artist, mediaProperties.AlbumTitle), true, isPlaying, controls.IsPreviousEnabled, controls.IsNextEnabled, controls.IsPlayEnabled || controls.IsPauseEnabled, _cachedThumbnail, effectivePosition.Ticks, timeline.EndTime.Ticks, hasTimeline);

        RaiseStateChanged(snapshot);
    }

    private void RaiseStateChanged(MediaPlaybackSnapshot snapshot)
    {
        if (_isDisposed) return;
        _dispatcherQueue.TryEnqueue(() => StateChanged?.Invoke(snapshot));
    }

    private static string BuildDescription(string? artist, string? albumTitle)
    {
        var hasArtist = !string.IsNullOrWhiteSpace(artist);
        var hasAlbum = !string.IsNullOrWhiteSpace(albumTitle);
        if (hasArtist && hasAlbum) return $"{artist} · {albumTitle}";
        if (hasArtist) return artist!;
        if (hasAlbum) return albumTitle!;
        return string.Empty;
    }

    private static TimeSpan CalculateEffectivePosition(GlobalSystemMediaTransportControlsSessionTimelineProperties timeline, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
    {
        if (playbackInfo.PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing) return timeline.Position;

        var rate = playbackInfo.PlaybackRate ?? 1.0;
        if (rate == 0) return timeline.Position;

        var elapsed = DateTimeOffset.Now - timeline.LastUpdatedTime;
        var effectivePosition = timeline.Position + TimeSpan.FromTicks((long)(elapsed.Ticks * rate));

        if (timeline.EndTime > TimeSpan.Zero && effectivePosition > timeline.EndTime) return timeline.EndTime;

        return effectivePosition;
    }

    private static async Task<BitmapImage?> LoadThumbnailAsync(IRandomAccessStreamReference thumbnailReference)
    {
        using var stream = await thumbnailReference.OpenReadAsync();
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);
        return bitmap;
    }
}
