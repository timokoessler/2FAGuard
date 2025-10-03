﻿using System.Windows;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Storage;

namespace Guard.WPF.Core
{
    class I18n
    {
        // Define supported languages in AppSettings enum too !!!
        private static readonly Dictionary<string, string> fullLanguageNames = new()
        {
            { "system", "System" },
            { "en", "English" },
            { "de", "Deutsch" },
            { "zh_cn", "大陆简体" },
            { "zh_tw", "中文(繁體)" },
            { "fr", "Français" },
            { "it", "Italiano" },
            { "el", "Ελληνικά" },
            { "ar", "العربية" },
            { "pt_br", "Português Brasileiro" },
            { "ja", "日本語" },
            { "cz", "Čeština" },
            { "ko", "한국어" },
            { "pl", "Polski" },
        };
        private static readonly LanguageSetting defaultLanguage = LanguageSetting.EN;
        private static LanguageSetting currentLanguage = defaultLanguage;
        private static ResourceDictionary? dict;

        public static LanguageSetting GetLanguage()
        {
            if (SettingsManager.Settings.Language != LanguageSetting.System)
            {
                return SettingsManager.Settings.Language;
            }

            LanguageSetting? systemLang = GetSystemLanguageIfSupported();
            if (systemLang != null)
            {
                return systemLang.Value;
            }

            return defaultLanguage;
        }

        public static LanguageSetting? GetSystemLanguageIfSupported()
        {
            string lang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            string regionCode = System
                .Globalization
                .RegionInfo
                .CurrentRegion
                .TwoLetterISORegionName;

            if (Enum.TryParse($"{lang}_{regionCode}", true, out LanguageSetting result))
            {
                return result;
            }

            if (Enum.TryParse(lang, true, out result))
            {
                return result;
            }

            return null;
        }

        public static void Init()
        {
            currentLanguage = GetLanguage();
            dict = new ResourceDictionary
            {
                Source = new Uri(
                    @"Resources/Strings." + currentLanguage.ToString().ToLowerInvariant() + ".xaml",
                    UriKind.Relative
                ),
            };
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        public static async void ChangeLanguage(LanguageSetting lang)
        {
            if (dict == null)
            {
                Log.Logger.Error("Can not change language, dict is null");
                return;
            }

            if (lang == currentLanguage)
            {
                return;
            }

            if (lang == LanguageSetting.System)
            {
                lang = GetSystemLanguageIfSupported() ?? defaultLanguage;
            }

            dict.Source = new Uri(
                @"Resources/Strings." + lang.ToString().ToLowerInvariant() + ".xaml",
                UriKind.Relative
            );
            currentLanguage = lang;
            SettingsManager.Settings.Language = lang;
            await SettingsManager.Save();
        }

        public static LanguageSetting GetCurrentLanguage()
        {
            return currentLanguage;
        }

        public static string GetString(string key)
        {
            if (dict == null)
            {
                return "Error!";
            }
            key = key.ToLowerInvariant();
            if (!key.StartsWith("i.", StringComparison.Ordinal))
            {
                key = "i." + key;
            }

            if (key.Equals("i.page.blank", StringComparison.Ordinal))
            {
                return "";
            }

            if (dict[key] is not string content)
            {
                Log.Logger.Warning("Missing translation for key: " + key);
                return $"??? {key} ???";
            }
            return content;
        }

        public static string GetFullLanguageName(string lang)
        {
            return fullLanguageNames[lang.ToLowerInvariant()];
        }
    }
}
