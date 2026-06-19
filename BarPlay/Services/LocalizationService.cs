using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Globalization;

namespace BarPlay.Services;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly List<string> s_installedLanguages = ["en-US", "ko-KR", "ja-JP", "zh-Hans", "zh-Hant"];

    private ResourceLoader _resourceLoader = new();

    public event EventHandler? LanguageChanged;

    public string GetString(string resourceName)
    {
        var normalizedResourceName = resourceName.Replace('.', '/');
        var localizedString = _resourceLoader.GetString(normalizedResourceName);
        return string.IsNullOrWhiteSpace(localizedString) ? resourceName : localizedString;
    }

    public string GetFormattedString(string resourceName, params object[] arguments) => string.Format(CultureInfo.CurrentCulture, GetString(resourceName), arguments);

    public void ApplyLanguage(string languageTag)
    {
        var resolvedTag = string.IsNullOrWhiteSpace(languageTag) ? GetDefaultLanguageTag() : languageTag;
        if (string.Equals(ApplicationLanguages.PrimaryLanguageOverride, resolvedTag, StringComparison.Ordinal)) return;

        ApplicationLanguages.PrimaryLanguageOverride = resolvedTag;
        ApplyCurrentThreadCultures(resolvedTag);
        _resourceLoader = new ResourceLoader();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ApplyCurrentThreadCultures(string languageTag)
    {
        try
        {
            var cultureInfo = CultureInfo.GetCultureInfo(languageTag);
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }
        catch (CultureNotFoundException) { }
    }

    private static string GetDefaultLanguageTag()
    {
        var installedUserInterfaceCultureName = CultureInfo.InstalledUICulture.Name;
        return s_installedLanguages.Contains(installedUserInterfaceCultureName) ? installedUserInterfaceCultureName : s_installedLanguages.First();
    }
}
