using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.WPF.Core;
using Guard.WPF.Core.Import.Importer;
using Guard.WPF.Views.Dialogs;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Pages.Add
{
    /// <summary>
    /// Interaktionslogik für AddOverview.xaml
    /// </summary>
    public partial class AddOverview : Page
    {
        private readonly MainWindow mainWindow;

        public AddOverview()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void Manual_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationContextManager.CurrentContext["action"] = "add";
            mainWindow.Navigate(typeof(TokenSettings), true);
        }

        private void Qr_Import_Click(object sender, RoutedEventArgs e)
        {
            Import(new QRFileImporter());
        }

        private void Clipboard_Click(object sender, RoutedEventArgs e)
        {
            Import(new ClipboardImporter());
        }

        private async void Import(IImporter importer)
        {
            try
            {
                int total;
                int duplicate;
                int tokenID;
                if (importer.Type == IImporter.ImportType.File)
                {
                    Microsoft.Win32.OpenFileDialog openFileDialog = new()
                    {
                        Filter = importer.SupportedFileExtensions,
                    };
                    bool? result = openFileDialog.ShowDialog();
                    if (result != true)
                    {
                        return;
                    }
                    byte[]? password = null;
                    if (importer.RequiresPassword(openFileDialog.FileName))
                    {
                        var dialog = new PasswordDialog(mainWindow.GetRootContentDialogPresenter());
                        dialog.Description.Text = I18n.GetString("import.password");
                        var dialogResult = await dialog.ShowAsync();

                        if (!dialogResult.Equals(ContentDialogResult.Primary))
                        {
                            return;
                        }
                        password = dialog.GetPassword();
                        if (password == null || password.Length == 0)
                        {
                            throw new Exception(I18n.GetString("import.password.invalid"));
                        }
                    }
                    (total, duplicate, tokenID) = importer.Parse(openFileDialog.FileName, password);
                }
                else if (importer.Type == IImporter.ImportType.Clipboard)
                {
                    if (importer.RequiresPassword(""))
                    {
                        throw new NotImplementedException(
                            "Importing password encrypted content from clipboard is not yet supported"
                        );
                    }
                    (total, duplicate, tokenID) = importer.Parse(null, null);
                }
                else if (importer.Type == IImporter.ImportType.ScreenCapture)
                {
                    if (importer.RequiresPassword(""))
                    {
                        throw new NotImplementedException(
                            "Importers that require a password are not supported for screen capture"
                        );
                    }
                    (total, duplicate, tokenID) = importer.Parse(null, null);
                }
                else
                {
                    throw new Exception("Invalid Importer Type");
                }

                if (total == 0)
                {
                    throw new Exception(I18n.GetString("import.notokens"));
                }

                if (total == 1)
                {
                    if (duplicate == 1)
                    {
                        throw new Exception(I18n.GetString("import.duplicate"));
                    }
                    NavigationContextManager.CurrentContext["type"] = "added";
                    NavigationContextManager.CurrentContext["tokenID"] = tokenID;
                }
                else
                {
                    NavigationContextManager.CurrentContext["type"] = "added-multiple";
                    NavigationContextManager.CurrentContext["tokenID"] = 0;
                    NavigationContextManager.CurrentContext["count"] = total;
                    NavigationContextManager.CurrentContext["duplicateCount"] = duplicate;
                }

                mainWindow.Navigate(typeof(TokenSuccessPage));
            }
            catch (Exception ex)
            {
                Log.Logger.Error(
                    "Import of {importer} failed: {message} {stacktrace}",
                    importer.Name,
                    ex.Message,
                    ex.StackTrace
                );
                string content = I18n.GetString("import.failed.content") + " ";

                if (ex.Message.Contains("Decryption failed (null)"))
                {
                    content = I18n.GetString("import.password.invalid");
                }
                else
                {
                    content += ex.Message;
                }

                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("import.failed.title"),
                    Content = content,
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400,
                }.ShowDialogAsync();
            }
        }

        private void GAuthenticator_Click(object sender, RoutedEventArgs e)
        {
            new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("import.gauthenticator"),
                Content = I18n.GetString("import.gauthenticator.msgbox.content")
                    .Replace("@n", "\n"),
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 500,
            }.ShowDialogAsync();
        }

        private void UriList_Click(object sender, RoutedEventArgs e)
        {
            Import(new UriListImporter());
        }

        private void Bitwarden_Click(object sender, RoutedEventArgs e)
        {
            Import(new BitwardenImporter());
        }

        private void AuthenticatorPro_Click(object sender, RoutedEventArgs e)
        {
            Import(new AuthenticatorProImporter());
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            Import(new BackupImporter());
        }

        private void TwoFas_Click(object sender, RoutedEventArgs e)
        {
            Import(new TwoFasImporter());
        }

        private void Aegis_Click(object sender, RoutedEventArgs e)
        {
            Import(new AegisAuthenticatorImporter());
        }

        private void QR_ScreenCapture_Click(object sender, RoutedEventArgs e)
        {
            Import(new QRScreenCaptureImporter());
        }
    }
}
