using Deskband11Lib.Core;

namespace BarPlay.Services;

public interface ISettingsService
{
    bool FocusPlayPauseButtonOnFlyoutOpen { get; set; }

    int PreferredMonitorIdentity { get; set; }

    TaskbarContentPlacement Placement { get; set; }
}