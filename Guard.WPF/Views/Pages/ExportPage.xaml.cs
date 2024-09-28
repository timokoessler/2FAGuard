using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.WPF.Core;
using Guard.WPF.Core.Export.Exporter;
using Guard.WPF.Views.Dialogs;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für AddOverview.xaml
    /// </summary>
    public partial class ExportPage : Page
    {
        private readonly MainWindow mainWindow;

        public ExportPage()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private async void Export(IExporter exporter)
        {
            try
            {
                if (exporter.Type == IExporter.ExportType.File)
                {
                    Microsoft.Win32.SaveFileDialog saveFileDialog =
                        new()
                        {
                            Filter = exporter.ExportFileExtensions,
                            AddExtension = true,
                            AddToRecent = false,
                            FileName = exporter.GetDefaultFileName()
                        };
                    bool? result = saveFileDialog.ShowDialog();

                    if (result != true)
                    {
                        return;
                    }
                    byte[]? password = null;
                    if (exporter.RequiresPassword())
                    {
                        var dialog = new PasswordDialog(mainWindow.GetRootContentDialogPresenter());
                        dialog.Description.Text = I18n.GetString("export.password");
                        var dialogResult = await dialog.ShowAsync();

                        if (!dialogResult.Equals(ContentDialogResult.Primary))
                        {
                            return;
                        }
                        password = dialog.GetPassword();
                        if (password == null || password.Length == 0)
                        {
                            throw new Exception(I18n.GetString("export.password.invalid"));
                        }
                    }
                    await exporter.Export(saveFileDialog.FileName, password);

                    Wpf.Ui.Controls.MessageBoxResult sucessDialogResult =
                        await new Wpf.Ui.Controls.MessageBox
                        {
                            Title = I18n.GetString("export.success.title"),
                            Content = I18n.GetString("export.success.content"),
                            CloseButtonText = I18n.GetString("dialog.close"),
                            PrimaryButtonText = I18n.GetString("export.success.open"),
                            MaxWidth = 400
                        }.ShowDialogAsync();

                    if (sucessDialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary)
                    {
                        ProcessStartInfo startInfo =
                            new()
                            {
                                Arguments = Path.GetDirectoryName(saveFileDialog.FileName),
                                FileName = "explorer.exe"
                            };

                        Process.Start(startInfo);
                    }
                }
                else
                {
                    throw new Exception("Invalid Exporter Type");
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
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("export.failed.title"),
                    Content = $"{I18n.GetString("export.failed.content")} {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
            }
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            Export(new BackupExporter());
        }

        private void UriList_Click(object sender, RoutedEventArgs e)
        {
            Export(new UriListExporter());
        }

        private void AuthenticatorPro_Click(object sender, RoutedEventArgs e)
        {
            Export(new AuthenticatorProExporter());
        }
    }
}
