using Guard.Core.Storage;

namespace Guard.Core
{
    public static class VersionCompatibility
    {
        // Increase the minimum app version when making breaking changes to prevent downgrades
        private static readonly Version minimumCompatibleVersion = new(1, 7, 0);

        /// <summary>
        /// Checks if the current app version is supported based on the minimum app version stored in settings.
        /// The minimum app version is stored in the settings to prevent older versions from running after
        /// running a newer version that has breaking changes.
        /// SettingsManager.Init() must be called before.
        /// Returns true if the current version is supported, false otherwise.
        /// </summary>
        /// <param name="currentVersion">The current app version</param>
        public static async Task<bool> Check(Version currentVersion)
        {
            bool settingsChanged = false;

            // If minimum version is not initialized, set it to the current minimum version
            if (
                SettingsManager.Settings.MinimumAppVersion.Major == 0
                && SettingsManager.Settings.MinimumAppVersion.Minor == 0
            )
            {
                SettingsManager.Settings.MinimumAppVersion = minimumCompatibleVersion;
                settingsChanged = true;
            }

            // Increase minimum app version if changed
            if (minimumCompatibleVersion > SettingsManager.Settings.MinimumAppVersion)
            {
                SettingsManager.Settings.MinimumAppVersion = minimumCompatibleVersion;
                settingsChanged = true;
            }

            bool isCompatible = currentVersion >= SettingsManager.Settings.MinimumAppVersion;

            // Update LastUsedAppVersion to the current version if app can be started
            if (isCompatible && SettingsManager.Settings.LastUsedAppVersion != currentVersion)
            {
                SettingsManager.Settings.LastUsedAppVersion = currentVersion;
                settingsChanged = true;
            }

            if (settingsChanged)
            {
                await SettingsManager.Save();
            }

            if (!isCompatible)
            {
                Log.Logger.Error(
                    "Preventing app start: Current version is older than minimum app version: {0} < {1}",
                    currentVersion,
                    SettingsManager.Settings.MinimumAppVersion
                );
            }

            return isCompatible;
        }
    }
}
