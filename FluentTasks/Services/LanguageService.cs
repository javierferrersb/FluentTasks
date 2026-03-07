using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.System.UserProfile;

namespace FluentTasks.UI.Services;

/// <summary>
/// Provides language detection and management for the application.
/// Detects the Windows display language and allows manual override.
/// </summary>
public class LanguageService
{
    /// <summary>
    /// Supported language codes that match available resource files.
    /// </summary>
    private static readonly List<string> s_supportedLanguages = new()
    {
        "en-US", // English (United States) - Default fallback
        "zh-CN", // Chinese (Simplified)
        "zh-TW", // Chinese (Traditional)
        "es-ES", // Spanish (Spain)
        "de-DE", // German (Germany)
        "fr-FR", // French (France)
        "it-IT", // Italian (Italy)
        "pt-PT", // Portuguese (Portugal)
        "ru-RU", // Russian (Russia)
        "ja-JP", // Japanese (Japan)
        "ko-KR", // Korean (Korea)
        "nl-NL", // Dutch (Netherlands)
        "pl-PL", // Polish (Poland)
        "tr-TR", // Turkish (Turkey)
        "sv-SE", // Swedish (Sweden)
        "da-DK", // Danish (Denmark)
        "fi-FI", // Finnish (Finland)
        "nb-NO", // Norwegian (Norway)
        "cs-CZ", // Czech (Czech Republic)
        "el-GR", // Greek (Greece)
        "he-IL", // Hebrew (Israel)
        "ar-SA", // Arabic (Saudi Arabia)
        "hi-IN", // Hindi (India)
        "th-TH", // Thai (Thailand)
        "vi-VN", // Vietnamese (Vietnam)
        "id-ID", // Indonesian (Indonesia)
        "ms-MY", // Malay (Malaysia)
        "uk-UA", // Ukrainian (Ukraine)
        "bg-BG", // Bulgarian (Bulgaria)
        "hr-HR", // Croatian (Croatia)
        "sk-SK", // Slovak (Slovakia)
        "sl-SI", // Slovenian (Slovenia)
        "sr-Latn-RS", // Serbian (Latin, Serbia)
        "ro-RO", // Romanian (Romania)
        "hu-HU", // Hungarian (Hungary)
        "ca-ES", // Catalan (Spain)
        "et-EE", // Estonian (Estonia)
        "lv-LV", // Latvian (Latvia)
        "lt-LT", // Lithuanian (Lithuania)
        "ga-IE", // Irish (Ireland)
        "mt-MT", // Maltese (Malta)
        "bn-BD", // Bengali (Bangladesh)
        "pa-PK", // Punjabi (Pakistan)
    };

    /// <summary>
    /// Gets a list of supported languages with display names.
    /// </summary>
    public static List<LanguageInfo> GetSupportedLanguages()
    {
        var languages = new List<LanguageInfo>();

        foreach (var code in s_supportedLanguages)
        {
            try
            {
                var culture = new CultureInfo(code);
                var displayName = culture.NativeName;
                languages.Add(new LanguageInfo(code, displayName));
            }
            catch
            {
                // Fallback for cultures that might not be available
                languages.Add(new LanguageInfo(code, code));
            }
        }

        // Add "Use Windows Setting" option at the top
        languages.Insert(0, new LanguageInfo("auto", "Use Windows setting"));

        return languages;
    }

    /// <summary>
    /// Detects the current Windows display language.
    /// Returns the best matching supported language.
    /// </summary>
    /// <returns>The detected language code (e.g., "en-US").</returns>
    public static string DetectWindowsLanguage()
    {
        try
        {
            // Get the primary language from Windows
            var primaryLanguage = GlobalizationPreferences.Languages.FirstOrDefault();

            if (string.IsNullOrEmpty(primaryLanguage))
            {
                return "en-US"; // Default fallback
            }

            // Try exact match first
            if (s_supportedLanguages.Contains(primaryLanguage))
            {
                return primaryLanguage;
            }

            // Try matching by language code only (e.g., "zh" from "zh-CN")
            var langCode = primaryLanguage.Split('-')[0];
            var match = s_supportedLanguages.FirstOrDefault(l => l.StartsWith(langCode));

            if (!string.IsNullOrEmpty(match))
            {
                return match;
            }

            // No match found, return default
            return "en-US";
        }
        catch
        {
            return "en-US";
        }
    }

    /// <summary>
    /// Gets the effective language code based on user preference.
    /// </summary>
    /// <param name="userPreference">User's language preference ("auto" or specific code).</param>
    /// <returns>The resolved language code.</returns>
    public static string GetEffectiveLanguage(string userPreference)
    {
        if (string.IsNullOrEmpty(userPreference) || userPreference == "auto")
        {
            return DetectWindowsLanguage();
        }

        // Validate user preference is supported
        return s_supportedLanguages.Contains(userPreference) ? userPreference : "en-US";
    }
}

/// <summary>
/// Represents a language option with code and display name.
/// </summary>
public class LanguageInfo
{
    public string Code { get; }
    public string DisplayName { get; }

    public LanguageInfo(string code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
