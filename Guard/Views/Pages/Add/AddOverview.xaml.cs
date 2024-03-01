using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Import;
using Guard.Core.Models;
using OtpNet;
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

                    if (qrText.StartsWith("otpauth-migration:"))
                    {
                        List<OTPUri> otpUris = GoogleAuthenticator.Parse(qrText);
                        if (otpUris.Count == 0)
                        {
                            throw new Exception(
                                "Google Authenticator migration failed because no tokens were found."
                            );
                        }
                        if (otpUris.Count == 1)
                        {
                            DBTOTPToken gDBToken = OTPUriHelper.ConvertToDBToken(otpUris[0]);
                            TokenManager.AddToken(gDBToken);
                            NavigationContextManager.CurrentContext["tokenID"] = gDBToken.Id;
                            NavigationContextManager.CurrentContext["type"] = "added";
                        }
                        else
                        {
                            foreach (OTPUri gOTPUri in otpUris)
                            {
                                DBTOTPToken gDBToken = OTPUriHelper.ConvertToDBToken(gOTPUri);
                                TokenManager.AddToken(gDBToken);
                            }
                            NavigationContextManager.CurrentContext["type"] = "added-multiple";
                            NavigationContextManager.CurrentContext["tokenID"] = 0;
                            NavigationContextManager.CurrentContext["count"] = otpUris.Count;
                        }

                        mainWindow.GetStatsClient()?.TrackEvent("TokenImportedGoogleQRFile");
                        mainWindow.Navigate(typeof(TokenSuccessPage));
                        return;
                    }

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
                var dbTokens =
                    ClipboardImport.Parse()
                    ?? throw new Exception(I18n.GetString("import.clipboard.invalid"));
                mainWindow.GetStatsClient()?.TrackEvent("TokenImportedClipboard");

                if (dbTokens.Count == 1)
                {
                    NavigationContextManager.CurrentContext["type"] = "added";
                    NavigationContextManager.CurrentContext["tokenID"] = dbTokens[0].Id;
                }
                else
                {
                    NavigationContextManager.CurrentContext["type"] = "added-multiple";
                    NavigationContextManager.CurrentContext["tokenID"] = 0;
                    NavigationContextManager.CurrentContext["count"] = dbTokens.Count;
                }
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

        private void GAuthenticator_Click(object sender, RoutedEventArgs e)
        {
            new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("i.import.gauthenticator"),
                Content = I18n.GetString("import.gauthenticator.msgbox.content"),
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 500
            }.ShowDialogAsync();
        }
    }
}
