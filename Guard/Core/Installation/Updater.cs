using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Guard.Core.Installation
{
    public class Updater
    {
        private static readonly string updateApiUrl = "https://2faguard.app/api/update";
        public static readonly HttpClient httpClient = new();
        private static readonly JsonSerializerOptions jsonSerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public class UpdateInfoDownloadUrls
        {
            public required string Installer { get; set; }
            public required string Portable { get; set; }
        }

        public class UpdateInfo
        {
            public required string Version { get; set; }
            public required UpdateInfoDownloadUrls Urls { get; set; }
        }

        internal static async Task<UpdateInfo?> CheckForUpdate()
        {
            Version? currentVersion = System
                .Reflection.Assembly.GetExecutingAssembly()
                .GetName()
                .Version;
            string currentVersionString = currentVersion?.ToString() ?? "";
            bool isPortable = InstallationInfo.IsPortable();

            try
            {
                string url =
                    $"{updateApiUrl}?current={currentVersionString}&isPortable={isPortable}";
                UpdateInfo updateInfo =
                    await httpClient.GetFromJsonAsync<UpdateInfo>(url, jsonSerializerOptions)
                    ?? throw new Exception("Failed to get update info (is null)");

                Version newVersion = new(updateInfo.Version);
                if (newVersion.CompareTo(currentVersion) <= 0)
                {
                    return null;
                }
                return updateInfo;
            }
            catch (Exception e)
            {
                Log.Logger.Error("Error while checking for updates: {0}", e.Message);
                return null;
            }
        }
    }
}
