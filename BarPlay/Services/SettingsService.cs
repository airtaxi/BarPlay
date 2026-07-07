using Windows.Storage;

namespace BarPlay.Services;

public sealed class SettingsService : ISettingsService
{
    private const string FocusPlayPauseButtonOnFlyoutOpenKey = "FocusPlayPauseButtonOnFlyoutOpen";
    private const string PreferredMonitorIdentityKey = "PreferredMonitorIdentity";

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

    private static bool ReadBool(string key, bool defaultValue) =>
        ApplicationData.Current.LocalSettings.Values[key] is bool value ? value : defaultValue;

    private static void WriteBool(string key, bool value) => ApplicationData.Current.LocalSettings.Values[key] = value;

    private static int ReadInt(string key, int defaultValue) =>
        ApplicationData.Current.LocalSettings.Values[key] is int value ? value : defaultValue;

    private static void WriteInt(string key, int value) => ApplicationData.Current.LocalSettings.Values[key] = value;
}