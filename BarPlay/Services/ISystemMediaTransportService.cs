using BarPlay.Models;

namespace BarPlay.Services;

public interface ISystemMediaTransportService : IDisposable
{
    event Action<MediaPlaybackSnapshot>? StateChanged;

    Task InitializeAsync();

    Task<bool> SkipPreviousAsync();

    Task<bool> SkipNextAsync();

    Task<bool> TogglePlayPauseAsync();

    Task<bool> SeekAsync(long positionTicks);
}
