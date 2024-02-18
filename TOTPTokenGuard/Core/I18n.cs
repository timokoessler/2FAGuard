using System.Windows;

namespace TOTPTokenGuard.Core
{
    class I18n
    {
        private static string[] supportedLanguages = { "en", "de" };
        private static Dictionary<string, string> fullLanguageNames = new Dictionary<string, string>
        {
            { "en", "English" },
            { "de", "Deutsch" }
        };
        private static string defaultLanguage = "en";
        private static string currentLanguage = defaultLanguage;
        private static ResourceDictionary? dict;

        public static string GetLanguage()
        {
            if (Properties.Settings.Default.lang != null)
            {
                string? selectedLang = Properties.Settings.Default.lang;
                if (
                    !string.IsNullOrEmpty(selectedLang) && supportedLanguages.Contains(selectedLang)
                )
                {
                    return selectedLang;
                }
            }

            string lang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if (!supportedLanguages.Contains(lang))
            {
                return defaultLanguage;
            }
            return lang;
        }

        public static void InitI18n()
        {
            dict = new ResourceDictionary
            {
                Source = new Uri(@"Resources/Strings." + GetLanguage() + ".xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(dict);
            currentLanguage = GetLanguage();
        }

        public static bool ChangeLanguage(string lang)
        {
            if (dict == null)
            {
                return false;
            }
            if (supportedLanguages.Contains(lang))
            {
                dict.Source = new Uri(@"Resources/Strings." + lang + ".xaml", UriKind.Relative);
                currentLanguage = lang;
                Properties.Settings.Default.lang = lang;
                Properties.Settings.Default.Save();
                return true;
            }
            return false;
        }

        public static string GetCurrentLanguage()
        {
            return currentLanguage;
        }

        public static string GetString(string key)
        {
            if (dict == null)
            {
                return "Error!";
            }
            string? content = dict[$"i.{key.ToLower()}"] as string;
            if (content == null)
            {
                return $"??? {key} ???";
            }
            return content;
        }

        public static string[] GetSupportedLanguages()
        {
            return supportedLanguages;
        }

        public static string GetFullLanguageName(string lang)
        {
            return fullLanguageNames[lang];
        }
    }
}
