using System.Globalization;

namespace PortraitModGenerator.Gui;

internal static class LocalizationManager
{
    public static readonly CultureInfo English = new("en");
    public static readonly CultureInfo Chinese = new("zh-CN");

    public static event Action? LanguageChanged;

    public static CultureInfo CurrentCulture { get; private set; } = Chinese;

    public static void SetLanguage(CultureInfo culture)
    {
        if (string.Equals(CurrentCulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        LanguageChanged?.Invoke();
    }
}
