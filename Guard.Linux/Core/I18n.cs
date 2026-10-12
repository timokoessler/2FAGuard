using System.Reflection;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Storage;

namespace Guard.WPF.Core
{
    /// <summary>
    /// Loads the translations of the Windows app (Guard.WPF/Resources/Strings.*.xaml) and
    /// exposes them as dynamic resources, so {DynamicResource i.xyz} works like in WPF.
    /// </summary>
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
            { "es", "Español" },
            { "sl", "Slovenščina" },
            { "tt", "Татарча" },
            { "et", "eesti keel" },
        };

        // Strings of the Windows app that describe Windows-only features
        private static readonly Dictionary<string, string> linuxEnglishOverrides = new()
        {
            { "i.settings.winhello", "System keyring" },
            {
                "i.settings.winhello.description",
                "Unlock the app with the login keyring of your desktop session"
            },
            {
                "i.settings.winhello.failed.content",
                "An error occurred while setting up the system keyring unlock:"
            },
            { "i.settings.autolock.description", "Lock the app when the screen is locked" },
            {
                "i.settings.autostart.description",
                "Automatically open the app when you log in"
            },
            { "i.welcome.hello", "System keyring or password" },
            { "i.welcome.hello.notavailable", "System keyring is not available" },
            {
                "i.welcome.hello.notavailable.content",
                "No Secret Service compatible keyring (e.g. GNOME Keyring or KWallet) was found. Please make sure that libsecret is installed and that a keyring is running in your desktop session."
            },
            {
                "i.welcome.portable.winhello.content",
                "If you are using the portable version of 2FAGuard on a USB stick or other portable storage device, you cannot automatically unlock the app with the system keyring of other users or on other devices. You must first activate the keyring unlock on other devices in the 2FAGuard settings. You can always log in with your password."
            },
            { "i.login.winhellobutton", "System keyring" },
            { "i.win.hello.request", "Authentication is required to unlock 2FAGuard" },
            {
                "i.settings.webauthn.notsupported",
                "libfido2 could not be loaded. Please install the package libfido2-1 to use security keys."
            },
            {
                "i.webauthn.dialog1.content2",
                "After entering the name, touch your security key twice: once to create the credential and once to verify it."
            },
        };

        // Linux-only strings (English fallback for all languages)
        private static readonly Dictionary<string, string> linuxExtraStrings = new()
        {
            { "i.linux.fido2.pin.title", "Security key PIN" },
            {
                "i.linux.fido2.pin.content",
                "Please enter the PIN of your security key."
            },
            { "i.linux.fido2.pin.input", "PIN" },
            { "i.linux.fido2.touch", "Please touch your security key…" },
            {
                "i.linux.fido2.nodevice",
                "No security key was found. Please connect your FIDO2 security key and try again."
            },
            { "i.linux.fido2.select", "Select security key" },
            {
                "i.linux.fido2.nouv",
                "The security key has no PIN and no built-in user verification. Please set a PIN for the security key first (e.g. with the manufacturer's tool or with 'fido2-token -S')."
            },
            {
                "i.linux.screenshot.denied",
                "The screenshot was canceled or denied by the desktop."
            },
            { "i.linux.notes.formatted", "Formatting of these notes is not shown and will be removed when you save." },
        };

        private const string winHelloKeyPrefix = "i.settings.winhello";

        private static readonly LanguageSetting defaultLanguage = LanguageSetting.EN;
        private static LanguageSetting currentLanguage = defaultLanguage;
        private static readonly ResourceDictionary dict = [];
        private static Dictionary<string, string>? englishStrings;

        public static LanguageSetting GetLanguage()
        {
            if (SettingsManager.Settings.Language != LanguageSetting.System)
            {
                return SettingsManager.Settings.Language;
            }

            return GetSystemLanguageIfSupported() ?? defaultLanguage;
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
            englishStrings = LoadStrings(LanguageSetting.EN);
            ApplyLanguage(currentLanguage);
            Application.Current!.Resources.MergedDictionaries.Add(dict);
        }

        public static async void ChangeLanguage(LanguageSetting lang)
        {
            if (lang == SettingsManager.Settings.Language)
            {
                return;
            }

            SettingsManager.Settings.Language = lang;
            if (lang == LanguageSetting.System)
            {
                lang = GetSystemLanguageIfSupported() ?? defaultLanguage;
            }

            if (lang != currentLanguage)
            {
                ApplyLanguage(lang);
                currentLanguage = lang;
            }
            await SettingsManager.Save();
        }

        public static LanguageSetting GetCurrentLanguage()
        {
            return currentLanguage;
        }

        public static string GetString(string key)
        {
            key = key.ToLowerInvariant();
            if (!key.StartsWith("i.", StringComparison.Ordinal))
            {
                key = "i." + key;
            }

            if (key.Equals("i.page.blank", StringComparison.Ordinal))
            {
                return "";
            }

            if (!dict.TryGetResource(key, null, out object? value) || value is not string content)
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

        private static void ApplyLanguage(LanguageSetting lang)
        {
            Dictionary<string, string> strings = new(englishStrings ?? []);
            if (lang != LanguageSetting.EN)
            {
                foreach (var (key, value) in LoadStrings(lang))
                {
                    strings[key] = value;
                }
            }

            foreach (var (key, value) in linuxEnglishOverrides)
            {
                if (lang == LanguageSetting.EN)
                {
                    strings[key] = value;
                }
                else if (strings.TryGetValue(key, out string? translated))
                {
                    strings[key] = AdaptWindowsWording(key, translated);
                }
            }
            foreach (var (key, value) in linuxExtraStrings)
            {
                strings.TryAdd(key, value);
            }

            foreach (var (key, value) in strings)
            {
                dict[key] = value;
            }
        }

        private static string AdaptWindowsWording(string key, string translated)
        {
            if (
                key.StartsWith(winHelloKeyPrefix, StringComparison.Ordinal)
                || key.Contains("hello", StringComparison.Ordinal)
            )
            {
                translated = translated.Replace("Windows Hello", "Keyring");
            }
            return translated.Replace("Windows", "Linux");
        }

        private static Dictionary<string, string> LoadStrings(LanguageSetting lang)
        {
            Dictionary<string, string> result = [];
            string resourceName = $"Strings.Strings.{lang.ToString().ToLowerInvariant()}.xaml";
            using Stream? stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Logger.Error("Translation resource {Name} not found", resourceName);
                return result;
            }

            XmlDocument doc = new();
            doc.Load(stream);
            const string xamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
            foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
            {
                if (node is not XmlElement element || element.LocalName != "String")
                {
                    continue;
                }
                string key = element.GetAttribute("Key", xamlNs);
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = element.InnerText;
                }
            }
            return result;
        }
    }
}
