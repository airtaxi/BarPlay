namespace BarPlay.Services;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;

    string GetString(string resourceName);

    string GetFormattedString(string resourceName, params object[] arguments);
}
