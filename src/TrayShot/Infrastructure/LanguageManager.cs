using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace TrayShot.Infrastructure;

public static class LanguageManager
{
    private const string LanguageDictUriPrefix = "pack://application:,,,/Resources/Languages/Strings.";

    public static void Initialize()
    {
        string configuredLang = AppSettings.Shared.Language;
        ApplyLanguage(configuredLang);
    }

    public static void ApplyLanguage(string languageCode)
    {
        if (Application.Current == null) return;

        string targetCulture = ResolveCultureCode(languageCode);
        string resourceUri = $"{LanguageDictUriPrefix}{targetCulture}.xaml";

        try
        {
            var newDict = new ResourceDictionary { Source = new Uri(resourceUri, UriKind.Absolute) };

            // Find existing language dictionary if present and replace it
            var existingLangDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Resources/Languages/Strings."));

            if (existingLangDict != null)
            {
                int index = Application.Current.Resources.MergedDictionaries.IndexOf(existingLangDict);
                Application.Current.Resources.MergedDictionaries[index] = newDict;
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(newDict);
            }

            Log.App.Info($"Applied UI Language: {targetCulture} (code: {languageCode})");
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed to load language dictionary for {languageCode} ({resourceUri}): {ex.Message}");
        }
    }

    private static string ResolveCultureCode(string languageCode)
    {
        if (string.Equals(languageCode, "system", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(languageCode))
        {
            var uiCulture = CultureInfo.CurrentUICulture.Name;
            if (uiCulture.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            {
                return "ko-KR";
            }
            return "en-US";
        }

        if (languageCode.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
        {
            return "ko-KR";
        }

        return "en-US";
    }

    public static string GetString(string key, params object[] args)
    {
        if (Application.Current != null && Application.Current.Resources.Contains(key))
        {
            string format = Application.Current.Resources[key]?.ToString() ?? key;
            if (args != null && args.Length > 0)
            {
                return string.Format(format, args);
            }
            return format;
        }
        return key;
    }
}
