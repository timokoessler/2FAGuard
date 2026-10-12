using System.Collections.Concurrent;
using Guard.Core.Storage;

namespace Guard.Core
{
    public static class I18n
    {
        private static readonly ConcurrentDictionary<string, string> translations = new();
        private static bool initialized = false;
        private static readonly object initLock = new();

        public static void Init()
        {
            lock (initLock)
            {
                if (initialized)
                    return;

                // Load default English translations
                LoadDefaultTranslations();
                initialized = true;
            }
        }

        private static void LoadDefaultTranslations()
        {
            // Import-related strings
            translations["import.password.invalid"] = "Invalid password";
            translations["import.notokens"] = "No tokens found in the import file";
            translations["import.duplicate"] = "Token already exists";
            translations["import.failed.title"] = "Import failed";
            translations["import.failed.content"] = "An error occurred during import: ";
            translations["import.password"] = "Enter password for encrypted backup";
            translations["import.hotp.notsupported"] = "HOTP tokens are not supported";
            translations["import.noqrfound"] = "No QR code found in image";
            translations["import.noqrfound.screen"] = "No QR code found on screen";
            translations["import.invalidqr"] = "Invalid QR code";
            translations["import.invalidqr.screen"] = "Invalid QR code on screen";
            translations["import.clipboard.invalid"] = "Invalid clipboard content";
            translations["import.clipboard.invalid.multifiles"] = "Multiple files in clipboard not supported";
            translations["import.authpro.invalid"] = "Invalid Authenticator Pro backup file";
            translations["import.authpro.legacy"] = "Legacy Authenticator Pro backup format not supported";
            translations["import.bitwarden.accountencryption"] = "Bitwarden account encryption not supported";
            translations["i.import.hotp.notsupported"] = "HOTP tokens are not supported";
            translations["td.invalidsecret"] = "Invalid secret";
            translations["i.td.customicon.tolarge"] = "Custom icon is too large (max 1MB)";
            translations["i.import.backup"] = "2FAGuard Backup";
            translations["i.import.backup.description"] = "Import a backup file that was created with 2FAGuard";
            translations["i.import.uri"] = "TOTP URI List";
            translations["i.import.uri.description"] = "Import a list of TOTP URIs";
            translations["i.import.bitwarden"] = "Bitwarden";
            translations["i.import.bitwarden.description"] = "Import tokens from the password manager Bitwarden";
            translations["i.import.authpro"] = "Authenticator Pro";
            translations["i.import.authpro.description"] = "Import a backup file from the app Stratum";
            translations["i.import.gauthenticator"] = "Google Authenticator";
            translations["i.import.gauthenticator.description"] = "Import tokens from the Google Authenticator app";
            translations["i.import.twofas"] = "2FAS";
            translations["i.import.twofas.description"] = "Import a backup file from the app 2FAS";
            translations["i.import.aegis"] = "Aegis Authenticator";
            translations["i.import.aegis.description"] = "Import an Aegis Authenticator export";
            translations["i.import.protonauthenticator"] = "Proton Authenticator";
            translations["i.import.protonauthenticator.description"] = "Import tokens from the Proton Authenticator app";
            translations["i.add.qr"] = "Import QR code";
            translations["i.add.qr.description"] = "Import a QR code from an image file";
            translations["i.add.qrscreen"] = "QR code from screen";
            translations["i.add.qrscreen.description"] = "Capture a QR code from your screen";
            translations["i.add.clipboard"] = "Clipboard";
            translations["i.add.clipboard.description"] = "Import from clipboard (QR code image or URI)";
            translations["i.add.manual"] = "Enter manually";
            translations["i.add.manual.description"] = "Manually enter token details";
            translations["i.add.import"] = "Import";
            translations["welcome.insecure.dialog.title"] = "Insecure setup";
            translations["welcome.insecure.dialog.content"] = "You are setting up 2FAGuard without a password. Your tokens will be stored unencrypted.";
            translations["welcome.insecure.dialog.continue"] = "Continue anyway";
            translations["welcome.hello.notavailable"] = "Windows Hello not available";
            translations["welcome.hello.notavailable.content"] = "Windows Hello is not available on this device.";
            translations["welcome.portable.winhello.title"] = "Portable mode";
            translations["welcome.portable.winhello.content"] = "Windows Hello is not available in portable mode.";
            translations["dialog.close"] = "Close";
            translations["dialog.next"] = "Next";
        }

        public static string GetString(string key)
        {
            if (!initialized)
            {
                Init();
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

            if (translations.TryGetValue(key, out var content))
            {
                return content;
            }

            Log.Logger.Warning("Missing translation for key: " + key);
            return $"??? {key} ???";
        }
    }
}