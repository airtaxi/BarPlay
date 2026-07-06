using Windows.Storage;

namespace BarPlay.Services;

public sealed class SettingsService : ISettingsService
{
    private const string FocusPlayPauseButtonOnFlyoutOpenKey = "FocusPlayPauseButtonOnFlyoutOpen";

    public bool FocusPlayPauseButtonOnFlyoutOpen
    {
        get => ReadBool(FocusPlayPauseButtonOnFlyoutOpenKey, defaultValue: true);
        set => WriteBool(FocusPlayPauseButtonOnFlyoutOpenKey, value);
    }

    private static bool ReadBool(string key, bool defaultValue) =>
        ApplicationData.Current.LocalSettings.Values[key] is bool value ? value : defaultValue;

    private static void WriteBool(string key, bool value) => ApplicationData.Current.LocalSettings.Values[key] = value;
}