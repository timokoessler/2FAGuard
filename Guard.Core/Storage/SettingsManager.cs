using System.Text.Json;
using Guard.Core.Models;

namespace Guard.Core.Storage
{
    public class SettingsManager
    {
        private static readonly string settingsFilePath = Path.Combine(
            InstallationContext.GetAppDataFolderPath(),
            "settings"
        );

        public static AppSettings Settings = new();

        private static volatile bool initialized;

        public static void Init()
        {
            if (File.Exists(settingsFilePath))
            {
                byte[] fileData = File.ReadAllBytes(settingsFilePath);
                string fileContent = System.Text.Encoding.UTF8.GetString(fileData);
                AppSettings? appSettings = JsonSerializer.Deserialize<AppSettings>(fileContent);
                if (appSettings != null)
                {
                    Settings = appSettings;
                }
            }
            initialized = true;
        }

        // Not async, so the exception is thrown synchronously even for fire-and-forget callers
        public static Task Save()
        {
            if (!initialized)
            {
                throw new InvalidOperationException(
                    "SettingsManager.Init() must be called before saving settings."
                );
            }
            return SaveInternal();
        }

        private static async Task SaveInternal()
        {
            string fileContent = JsonSerializer.Serialize(Settings);
            byte[] fileData = System.Text.Encoding.UTF8.GetBytes(fileContent);
            await SafeFileWriter.SaveFileAsync(settingsFilePath, fileData);
        }
    }
}
