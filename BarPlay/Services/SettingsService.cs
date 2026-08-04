using BarPlay.Models;
using Deskband11Lib.Core;
using Windows.Storage;

namespace BarPlay.Services;

public sealed class SettingsService : ISettingsService
{
    private const string FocusPlayPauseButtonOnFlyoutOpenKey = "FocusPlayPauseButtonOnFlyoutOpen";
    private const string PreferredMonitorIdentityKey = "PreferredMonitorIdentity";
    private const string PlacementKey = "Placement";
    private const string WidthKey = "Width";

    public bool FocusPlayPauseButtonOnFlyoutOpen
    {
        get => ReadBool(FocusPlayPauseButtonOnFlyoutOpenKey, defaultValue: true);
        set => WriteBool(FocusPlayPauseButtonOnFlyoutOpenKey, value);
    }

    public int PreferredMonitorIdentity
    {
        get => ReadInt(PreferredMonitorIdentityKey, defaultValue: 0);
        set => WriteInt(PreferredMonitorIdentityKey, value);
    }

    public TaskbarContentPlacement Placement
    {
        get => (TaskbarContentPlacement)ReadInt(PlacementKey, defaultValue: (int)TaskbarContentPlacement.Auto);
        set => WriteInt(PlacementKey, (int)value);
    }

    public TaskbarWidth Width
    {
        get => (TaskbarWidth)ReadInt(WidthKey, defaultValue: (int)TaskbarWidth.Wide);
        set => WriteInt(WidthKey, (int)value);
    }

    private static bool ReadBool(string key, bool defaultValue) =>
        ApplicationData.Current.LocalSettings.Values[key] is bool value ? value : defaultValue;

    private static void WriteBool(string key, bool value) => ApplicationData.Current.LocalSettings.Values[key] = value;

    private static int ReadInt(string key, int defaultValue) =>
        ApplicationData.Current.LocalSettings.Values[key] is int value ? value : defaultValue;

    private static void WriteInt(string key, int value) => ApplicationData.Current.LocalSettings.Values[key] = value;
}