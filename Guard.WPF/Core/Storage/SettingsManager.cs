using System.Text.Json;
using Guard.WPF.Core.Models;
using Guard.Core;

namespace Guard.WPF.Core.Storage
{
    internal class SettingsManager
    {
        private static readonly string settingsFilePath = System.IO.Path.Combine(
            InstallationContext.GetAppDataFolderPath(),
            "settings"
        );

        public static AppSettings Settings = new();

        public static void Init()
        {
            if (System.IO.File.Exists(settingsFilePath))
            {
                byte[] fileData = System.IO.File.ReadAllBytes(settingsFilePath);
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
