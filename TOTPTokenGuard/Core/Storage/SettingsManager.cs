using System.Text.Json;
using TOTPTokenGuard.Core.Models;

namespace TOTPTokenGuard.Core.Storage
{
    internal class SettingsManager
    {
        private static readonly string settingsFilePath = System.IO.Path.Combine(
            Utils.GetAppDataFolderPath(),
            "settings"
        );

        public static AppSettings Settings = new AppSettings();

        public static async Task Init()
        {
            if (System.IO.File.Exists(settingsFilePath))
            {
                byte[] fileData = await System.IO.File.ReadAllBytesAsync(settingsFilePath);
                string fileContent = System.Text.Encoding.UTF8.GetString(fileData);
                AppSettings? appSettings = JsonSerializer.Deserialize<AppSettings>(fileContent);
                if (appSettings != null)
                {
                    Settings = appSettings;
                }
            }
        }

        public static async Task Save()
        {
            string fileContent = JsonSerializer.Serialize(Settings);
            byte[] fileData = System.Text.Encoding.UTF8.GetBytes(fileContent);
            await System.IO.File.WriteAllBytesAsync(settingsFilePath, fileData);
        }
    }
}
