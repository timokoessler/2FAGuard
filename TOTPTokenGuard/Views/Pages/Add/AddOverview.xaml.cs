using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Import;
using TOTPTokenGuard.Core.Models;
using ZXing.Aztec.Internal;

namespace TOTPTokenGuard.Views.Pages.Add
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
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog =
                    new() { Filter = "Image (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png" };
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    string? qrText =
                        QRCode.ParseQRFile(openFileDialog.FileName)
                        ?? throw new Exception(I18n.GetString("import.noqrfound"));
                    OTPUri otpUri = OTPUriHelper.Parse(qrText);
                    DBTOTPToken dbToken = OTPUriHelper.ConvertToDBToken(otpUri);
                    TokenManager.AddToken(dbToken);

                    mainWindow.GetStatsClient()?.TrackEvent("TokenImportedQRFile");

                    NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                    NavigationContextManager.CurrentContext["type"] = "added";
                    mainWindow.Navigate(typeof(TokenSuccessPage));
                }
            }
            catch (Exception ex)
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("import.failed.title"),
                    Content = $"{I18n.GetString("import.failed.content")} {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
            }
        }

        private void Clipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dbToken = ClipboardImport.Parse();
                if (dbToken == null)
                {
                    throw new Exception(I18n.GetString("import.clipboard.invalid"));
                }
                mainWindow.GetStatsClient()?.TrackEvent("TokenImportedClipboard");
                NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                NavigationContextManager.CurrentContext["type"] = "added";
                mainWindow.Navigate(typeof(TokenSuccessPage));
            }
            catch (Exception ex)
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("import.failed.title"),
                    Content = $"{I18n.GetString("import.failed.content")} {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
            }
        }
    }
}
