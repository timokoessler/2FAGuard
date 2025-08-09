using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Guard.Core;
using Guard.Core.Security;

namespace Guard.WPF.Core
{
    internal class ExceptionHandler
    {
        private static readonly string reportApiUrl = "https://2faguard.app/api/report";
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        internal static async void OnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs args
        )
        {
            Exception e = (Exception)args.ExceptionObject;
            string content = $"{I18n.GetString("error.unhandled.content")}\n\n{e.Message}";
            if (e.StackTrace != null)
            {
                content += e.StackTrace[..450] + "...";
            }

            Log.Logger.Error("Unhandled Exception {Exception}", e);

            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("error.unhandled.title"),
                Content = content,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = I18n.GetString("error.unhandled.openbug"),
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 600,
            };

            var result = await uiMessageBox.ShowDialogAsync();
            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                /*SendBugReport($"{e.Message}\n\n{e.StackTrace}");

                var uiMessageBox2 = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("error.unhandled.title"),
                    Content = I18n.GetString("error.unhandled.github"),
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = I18n.GetString("error.unhandled.github.open"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 600
                };

                var result2 = await uiMessageBox2.ShowDialogAsync();
                if (result2 == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {*/
                Process.Start(
                    new ProcessStartInfo(GetGitHubIssueUrl($"{e.Message}\n\n{e.StackTrace}"))
                    {
                        UseShellExecute = true,
                    }
                );
                //}
            }
            Environment.Exit(1);
        }

        private static async void SendBugReport(string errorStack)
        {
            try
            {
                var content = new
                {
                    type = "crash_report",
                    id = Convert.ToHexString(
                        SHA256.HashData(Encoding.UTF8.GetBytes(Auth.GetInstallationID()))
                    ),
                    appVersion = InstallationContext.GetVersionString(),
                    osBuild = InstallationContext.GetOsBuildVersion(),
                    appEdition = InstallationContext.GetAppEditionString(),
                    locale = InstallationContext.GetLocaleCode(),
                    stackTrace = errorStack,
                };
                var httpClient = HTTP.GetHttpClient();
                var response = await httpClient.PostAsJsonAsync(
                    reportApiUrl,
                    content,
                    jsonSerializerOptions
                );

                if (!response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    throw new Exception(
                        $"Failed to send bug report: HTTP {response.StatusCode} {responseContent}"
                    );
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error("Failed to send bug report {Exception}", e);
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("error"),
                    Content =
                        "Failed to send bug report. Please report the issue manually on GitHub.",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400,
                }.ShowDialogAsync();
            }
        }

        private static string GetGitHubIssueUrl(string errorStack)
        {
            string windowsVersion = HttpUtility.UrlEncode(InstallationContext.GetOsVersionString());
            string errorMessage = HttpUtility.UrlEncode(errorStack);
            string version = HttpUtility.UrlEncode(InstallationContext.GetVersionString());
            string installationType = HttpUtility.UrlEncode(
                InstallationContext.GetInstallationTypeString()
            );

            return $"https://github.com/timokoessler/2FAGuard/issues/new?template=bug.yml&title=%5BBug%5D%3A+&error-message={errorMessage}&win-version={windowsVersion}&app-version={version}%20({installationType})";
        }
    }
}
