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

        public static void Init()
        {
            string backupFilePath = $"{settingsFilePath}.bak";
            if (!File.Exists(settingsFilePath))
            {
                Settings = File.Exists(backupFilePath)
                    ? RestoreBackup(settingsFilePath, backupFilePath)
                    : new();
                return;
            }

            try
            {
                Settings = ReadSettings(settingsFilePath);
            }
            catch (JsonException primaryException) when (File.Exists(backupFilePath))
            {
                try
                {
                    Settings = RestoreBackup(settingsFilePath, backupFilePath);
                }
                catch (JsonException backupException)
                {
                    throw new JsonException(
                        "Both the primary settings file and its backup are invalid.",
                        new AggregateException(primaryException, backupException)
                    );
                }
            }
        }

        private static AppSettings ReadSettings(string filePath)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            AppSettings? appSettings = JsonSerializer.Deserialize<AppSettings>(fileData);
            return appSettings ?? throw new JsonException("The settings file contains null.");
        }

        private static AppSettings RestoreBackup(string filePath, string backupFilePath)
        {
            AppSettings appSettings = ReadSettings(backupFilePath);
            SafeFileWriter.RestoreFile(filePath, File.ReadAllBytes(backupFilePath));
            return appSettings;
        }

        public static async Task Save()
        {
            string fileContent = JsonSerializer.Serialize(Settings);
            byte[] fileData = System.Text.Encoding.UTF8.GetBytes(fileContent);
            await SafeFileWriter.SaveFileAsync(settingsFilePath, fileData);
        }
    }
}
