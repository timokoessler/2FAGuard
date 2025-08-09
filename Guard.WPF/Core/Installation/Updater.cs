using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using Guard.Core;
using Microsoft.Security.Extensions;

namespace Guard.WPF.Core.Installation
{
    public class Updater
    {
        public static readonly string updateApiUrl = "https://2faguard.app/api/update";
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static DateTime lastUpdateCheck = DateTime.MinValue;
        private static UpdateInfo? lastUpdateInfo = null;

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
            bool isPortable = InstallationContext.IsPortable();

            // Cache last update check for 1 hour
            if (lastUpdateCheck.AddHours(1) > DateTime.Now)
            {
                return lastUpdateInfo;
            }

            try
            {
                var httpClient = HTTP.GetHttpClient();
                string url =
                    $"{updateApiUrl}?current={currentVersionString}&isPortable={isPortable}";
                UpdateInfo updateInfo =
                    await httpClient.GetFromJsonAsync<UpdateInfo>(url, jsonSerializerOptions)
                    ?? throw new Exception("Failed to get update info (is null)");

                lastUpdateCheck = DateTime.Now;

                Version newVersion = new(updateInfo.Version);
                if (newVersion.CompareTo(currentVersion) <= 0)
                {
                    lastUpdateInfo = null;
                    return null;
                }
                lastUpdateInfo = updateInfo;
                return updateInfo;
            }
            catch (Exception e)
            {
                lastUpdateInfo = null;
                Log.Logger.Error("Error while checking for updates: {0}", e.Message);
                return null;
            }
        }

        // Checks if the file is signed with a valid code signing certificate
        // https://stackoverflow.com/a/75291260/8425220
        internal static bool IsFileTrusted(string path)
        {
            using FileStream fs = File.OpenRead(path);
            FileSignatureInfo sigInfo = FileSignatureInfo.GetFromFileStream(fs);

            return sigInfo.State == SignatureState.SignedAndTrusted;
        }

        internal static async Task Update(UpdateInfo updateInfo)
        {
            bool isPortable = InstallationContext.IsPortable();
            string downloadUrl = isPortable ? updateInfo.Urls.Portable : updateInfo.Urls.Installer;

            string downloadFileName = Path.GetFullPath(
                isPortable
                    ? Path.Combine(
                        AppContext.BaseDirectory,
                        $"2FAGuard-Portable-{updateInfo.Version}.zip"
                    )
                    : Path.Combine(Path.GetTempPath(), $"2FAGuard-Updater-{updateInfo.Version}.exe")
            );

            Log.Logger.Information(
                "Downloading update from {0} to {1}",
                downloadUrl,
                downloadFileName
            );
            if (File.Exists(downloadFileName))
            {
                File.Delete(downloadFileName);
            }

            string portableExePath = Path.Combine(
                AppContext.BaseDirectory,
                $"2FAGuard-Portable-{updateInfo.Version}.exe"
            );
            if (isPortable && File.Exists(portableExePath))
            {
                throw new Exception(
                    "You have already downloaded the newest portable version. Please start the new version instead of the old one."
                );
            }

            var httpClient = HTTP.GetHttpClient();
            using var stream = await httpClient.GetStreamAsync(downloadUrl);
            using FileStream fileStream = new(
                downloadFileName,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None
            );
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            stream.Close();

            string startFilePath = downloadFileName;
            string arguments = "/SILENT";

            // If is portable version
            if (isPortable)
            {
                string extractDir = Path.Combine(
                    AppContext.BaseDirectory,
                    $"2FAGuard-Portable-Update-{updateInfo.Version}-Temp"
                );

                await Task.Run(() =>
                {
                    if (Directory.Exists(extractDir))
                    {
                        Directory.Delete(extractDir, true);
                    }
                    Directory.CreateDirectory(extractDir);
                    System.IO.Compression.ZipFile.ExtractToDirectory(downloadFileName, extractDir);
                    string[] extractedExePaths = Directory.GetFiles(
                        extractDir,
                        "*.exe",
                        SearchOption.AllDirectories
                    );
                    if (extractedExePaths.Length == 0)
                    {
                        throw new Exception("Did not find any executable in the zip file");
                    }

                    foreach (string exePath in extractedExePaths)
                    {
                        if (!IsFileTrusted(exePath))
                        {
                            throw new Exception(
                                "The downloaded archive contains a file that is not signed and therefore not trusted. This may be a error or a security risk."
                            );
                        }

                        string fileName = Path.GetFileName(exePath);
                        if (fileName.ToLower().Equals("2faguard-portable.exe"))
                        {
                            fileName = $"2FAGuard-Portable-{updateInfo.Version}.exe";
                        }

                        File.Move(exePath, Path.Combine(AppContext.BaseDirectory, fileName), true);
                    }

                    File.Delete(downloadFileName);
                    Directory.Delete(extractDir, true);
                });
                startFilePath = portableExePath;
                arguments = $"--updated-from {InstallationContext.GetVersionString()} --portable";
            }

            if (!IsFileTrusted(startFilePath))
            {
                throw new Exception(
                    "The downloaded file is not signed by the developer and therefore not trusted. This may be a error or a security risk."
                );
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(startFilePath)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = arguments,
                },
            };
            process.Start();
            Application.Current.Shutdown();
        }
    }
}
