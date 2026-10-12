using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Guard.Core;
using Guard.Linux.Core;
using Guard.WPF.Core;
using Guard.WPF.Core.Export.Exporter;

namespace Guard.Linux.Views.Pages
{
    public partial class ExportPage : UserControl
    {
        private bool exportRunning;

        public ExportPage()
        {
            InitializeComponent();
        }

        private async void Export(IExporter exporter)
        {
            if (exportRunning)
            {
                return;
            }
            exportRunning = true;
            try
            {
                if (!exporter.RequiresPassword() && RegistrySettings.PreventUnencryptedExports())
                {
                    throw new Exception(I18n.GetString("i.export.failed.unencrypted"));
                }

                if (exporter.Type != IExporter.ExportType.File)
                {
                    throw new Exception("Invalid Exporter Type");
                }

                string? path = await FileDialogs.SaveFile(
                    exporter.ExportFileExtensions,
                    exporter.GetDefaultFileName()
                );
                if (path == null)
                {
                    return;
                }

                byte[]? password = null;
                if (exporter.RequiresPassword())
                {
                    password = await Dialogs.PasswordDialog(I18n.GetString("export.password"));
                    if (password == null)
                    {
                        return;
                    }
                    if (password.Length == 0)
                    {
                        throw new Exception(I18n.GetString("export.password.invalid"));
                    }
                }

                await exporter.Export(path, password);

                var result = await Dialogs.MessageBox(
                    I18n.GetString("export.success.title"),
                    I18n.GetString("export.success.content"),
                    primaryButtonText: I18n.GetString("export.success.open")
                );

                string? directory = Path.GetDirectoryName(path);
                if (result == DialogResult.Primary && directory != null)
                {
                    ProcessStartInfo startInfo = new("xdg-open") { UseShellExecute = false };
                    startInfo.ArgumentList.Add(directory);
                    Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(
                    "Export to {exporter} failed: {message} {stacktrace}",
                    exporter.Name,
                    ex.Message,
                    ex.StackTrace
                );
                await Dialogs.MessageBox(
                    I18n.GetString("export.failed.title"),
                    $"{I18n.GetString("export.failed.content")} {ex.Message}"
                );
            }
            finally
            {
                exportRunning = false;
            }
        }

        private void Backup_Click(object? sender, RoutedEventArgs e) => Export(new BackupExporter());

        private void UriList_Click(object? sender, RoutedEventArgs e) => Export(new UriListExporter());

        private void AuthenticatorPro_Click(object? sender, RoutedEventArgs e) =>
            Export(new AuthenticatorProExporter());
    }
}
