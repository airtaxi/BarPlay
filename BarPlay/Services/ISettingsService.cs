namespace BarPlay.Services;

public interface ISettingsService
{
    bool FocusPlayPauseButtonOnFlyoutOpen { get; set; }

    int PreferredMonitorIdentity { get; set; }
}