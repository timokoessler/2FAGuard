using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Guard.Core;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.WPF.Core.Installation;

namespace Guard.WPF.Core
{
    internal class Stats
    {
        private static readonly string statsApiUrl = "https://2faguard.app/api/stats";
        private static readonly JsonSerializerOptions jsonSerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        internal enum EventType
        {
            AppStarted,
            SetupCompleted,
            UpdateStarted,
        }

        private static string EventTypeToString(EventType type)
        {
            return type switch
            {
                EventType.AppStarted => "app_started",
                EventType.SetupCompleted => "setup_completed",
                EventType.UpdateStarted => "update_started",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        internal static async void TrackEvent(EventType type)
        {
            if (InstallationInfo.IsInDebugMode())
            {
                return;
            }

            if (type == EventType.AppStarted)
            {
                if (
                    DateTime.Compare(
                        SettingsManager.Settings.LastAppStartEvent.Date,
                        DateTime.Now.Date
                    ) == 0
                )
                {
                    return;
                }
            }

            try
            {
                var content = new
                {
                    type = EventTypeToString(type),
                    id = Convert.ToHexString(
                        SHA256.HashData(Encoding.UTF8.GetBytes(Auth.GetInstallationID()))
                    ),
                    appVersion = InstallationContext.GetVersionString(),
                    osBuild = InstallationContext.GetOsBuildVersion(),
                    appEdition = InstallationContext.GetAppEditionString(),
                    locale = InstallationContext.GetLocaleCode()
                };
                var httpClient = HTTP.GetHttpClient();
                var response =
                    await httpClient.PostAsJsonAsync(statsApiUrl, content, jsonSerializerOptions)
                    ?? throw new Exception("Failed to get update info (is null)");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Failed to get update info (status code: {response.StatusCode})"
                    );
                }

                if (type == EventType.AppStarted)
                {
                    SettingsManager.Settings.LastAppStartEvent = DateTime.Now;
                    _ = SettingsManager.Save();
                }
            }
            catch
            {
                //
            }
        }
    }
}
