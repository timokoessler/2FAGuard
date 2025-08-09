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
        internal static async void OnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs args
        )
        {
            Exception e = (Exception)args.ExceptionObject;
            string content = $"{I18n.GetString("error.unhandled.content")}\n\n";

            content += e.ToString()[..Math.Min(e.ToString().Length, 650)] + "...";

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
                Process.Start(
                    new ProcessStartInfo(GetGitHubIssueUrl(e.Message, e.ToString()))
                    {
                        UseShellExecute = true,
                    }
                );
            }
            Environment.Exit(1);
        }

        private static string GetGitHubIssueUrl(string errorMessage, string fullError)
        {
            string installationType = HttpUtility.UrlEncode(
                InstallationContext.GetInstallationTypeString()
            );

            var urlParams = new Dictionary<string, string>
            {
                { "template", "bug.yml" },
                { "title", HttpUtility.UrlEncode($"🐛 Bug Report: {errorMessage}") },
                { "error-message", HttpUtility.UrlEncode(fullError) },
                { "win-version", HttpUtility.UrlEncode(InstallationContext.GetOsVersionString()) },
                { "app-version", HttpUtility.UrlEncode(InstallationContext.GetVersionString()) },
                { "app-edition", installationType },
            };

            string queryString = string.Join("&", urlParams.Select(kv => $"{kv.Key}={kv.Value}"));

            return $"https://github.com/timokoessler/2FAGuard/issues/new?{queryString}";
        }
    }
}
