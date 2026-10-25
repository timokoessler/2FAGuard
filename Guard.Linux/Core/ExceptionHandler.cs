using System.Diagnostics;
using System.Web;
using Avalonia.Threading;
using Guard.Core;
using Guard.WPF.Core;

namespace Guard.Linux.Core
{
    internal class ExceptionHandler
    {
        private static bool handling;

        internal static void Register()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                Log.Logger.Fatal("Unhandled Exception {Exception}", args.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Log.Logger.Error("Unobserved task exception {Exception}", args.Exception);
                args.SetObserved();
            };
            Dispatcher.UIThread.UnhandledException += (_, args) =>
            {
                args.Handled = true;
                OnUnhandledException(args.Exception);
            };
        }

        private static async void OnUnhandledException(Exception e)
        {
            Log.Logger.Error("Unhandled Exception {Exception}", e);
            if (handling)
            {
                return;
            }
            handling = true;
            try
            {
                string content = $"{I18n.GetString("error.unhandled.content")}\n\n";
                content += e.ToString()[..Math.Min(e.ToString().Length, 650)] + "...";

                var result = await Dialogs.MessageBox(
                    I18n.GetString("error.unhandled.title"),
                    content,
                    primaryButtonText: I18n.GetString("error.unhandled.openbug"),
                    maxWidth: 600
                );
                if (result == DialogResult.Primary)
                {
                    OpenUrl(GetGitHubIssueUrl(e.Message, e.ToString()));
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to show error dialog: {0}", ex.Message);
            }
            Environment.Exit(1);
        }

        internal static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo("xdg-open", url) { UseShellExecute = false });
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to open url: {0}", ex.Message);
            }
        }

        private static string GetGitHubIssueUrl(string errorMessage, string fullError)
        {
            var urlParams = new Dictionary<string, string>
            {
                { "template", "bug.yml" },
                { "title", HttpUtility.UrlEncode($"🐛 Bug Report (Linux): {errorMessage}") },
                { "error-message", HttpUtility.UrlEncode(fullError) },
                { "win-version", HttpUtility.UrlEncode(InstallationContext.GetOsVersionString()) },
                { "app-version", HttpUtility.UrlEncode(InstallationContext.GetVersionString()) },
                {
                    "app-edition",
                    HttpUtility.UrlEncode(InstallationContext.GetInstallationTypeString())
                },
            };

            string queryString = string.Join("&", urlParams.Select(kv => $"{kv.Key}={kv.Value}"));
            return $"https://github.com/timokoessler/2FAGuard/issues/new?{queryString}";
        }
    }
}
