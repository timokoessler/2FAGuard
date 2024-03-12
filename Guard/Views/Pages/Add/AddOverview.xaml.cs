using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Import;
using Guard.Core.Import.Importer;
using Guard.Core.Models;
using Guard.Views.Controls;
using OtpNet;
using Wpf.Ui.Controls;
using ZXing.Aztec.Internal;

namespace Guard.Views.Pages.Add
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
            mainWindow.Navigate(typeof(TokenSettings));
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
            int total = 0,
                duplicate = 0,
                tokenID = 0;
            try
            {
                if (importer.Type == IImporter.ImportType.File)
                {
                    Microsoft.Win32.OpenFileDialog openFileDialog =
                        new() { Filter = importer.SupportedFileExtensions };
                    bool? result = openFileDialog.ShowDialog();
                    if (result != true)
                    {
                        return;
                    }
                    string? password = null;
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
                        if (string.IsNullOrEmpty(password))
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

                mainWindow.GetStatsClient()?.TrackEvent("TokenImported" + importer.Name);
                mainWindow.Navigate(typeof(TokenSuccessPage));
            }
            catch (Exception ex)
            {
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("import.failed.title"),
                    Content = $"{I18n.GetString("import.failed.content")} {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
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
                MaxWidth = 500
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
    }
}
