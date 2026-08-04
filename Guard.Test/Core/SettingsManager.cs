using System.Text.Json;
using Guard.Core.Models;
using Guard.Core.Storage;

namespace Guard.Test.Core
{
    public class SettingsManagerTests : IDisposable
    {
        private readonly string testDirectory;
        private readonly string settingsPath;
        private readonly byte[]? originalSettings;
        private readonly byte[]? originalBackup;

        public SettingsManagerTests()
        {
            InstallationContext.Init(InstallationType.CLASSIC_PORTABLE, new Version(1, 0));
            testDirectory = InstallationContext.GetAppDataFolderPath();
            settingsPath = Path.Combine(testDirectory, "settings");
            originalSettings = File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null;
            originalBackup = File.Exists($"{settingsPath}.bak")
                ? File.ReadAllBytes($"{settingsPath}.bak")
                : null;
            Directory.CreateDirectory(testDirectory);
        }

        [Fact]
        public void RecoversCorruptedSettingsFromBackup()
        {
            string backupPath = $"{settingsPath}.bak";
            AppSettings expected = new()
            {
                Theme = ThemeSetting.Dark,
                Language = LanguageSetting.DE,
                MinimizeToTray = true,
            };
            string backupContent = JsonSerializer.Serialize(expected);

            File.WriteAllBytes(settingsPath, new byte[4096]);
            File.WriteAllText(backupPath, backupContent);

            Guard.Core.Storage.SettingsManager.Init();
            AppSettings actual = Guard.Core.Storage.SettingsManager.Settings;

            Assert.Equal(expected.Theme, actual.Theme);
            Assert.Equal(expected.Language, actual.Language);
            Assert.Equal(expected.MinimizeToTray, actual.MinimizeToTray);
            Assert.Equal(backupContent, File.ReadAllText(settingsPath));
            Assert.Equal(backupContent, File.ReadAllText(backupPath));
        }

        [Fact]
        public void ThrowsWhenSettingsAndBackupAreBothCorrupted()
        {
            File.WriteAllBytes(settingsPath, new byte[4096]);
            File.WriteAllText($"{settingsPath}.bak", "not-json");

            Assert.Throws<JsonException>(() =>
                Guard.Core.Storage.SettingsManager.Init()
            );
        }

        public void Dispose()
        {
            RestoreFile(settingsPath, originalSettings);
            RestoreFile($"{settingsPath}.bak", originalBackup);
            Guard.Core.Storage.SettingsManager.Settings = new();
            GC.SuppressFinalize(this);
        }

        private static void RestoreFile(string path, byte[]? content)
        {
            if (content == null)
            {
                File.Delete(path);
                return;
            }
            File.WriteAllBytes(path, content);
        }
    }
}
