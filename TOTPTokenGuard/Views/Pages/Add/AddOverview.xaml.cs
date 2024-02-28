using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Import;
using TOTPTokenGuard.Core.Models;

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

        private async void Qr_Screen_Click(object sender, RoutedEventArgs e)
        {
            /*if (!GraphicsCaptureSession.IsSupported())
            {
                throw new Exception("GraphicsCaptureSession not supported");
            }

            var interopWindow = new WindowInteropHelper(mainWindow);
            IntPtr Hwnd = interopWindow.Handle;

            var picker = new GraphicsCapturePicker();
            InitializeWithWindow.Initialize(picker, Hwnd);

            var item = await picker.PickSingleItemAsync();

            if (item != null)
            {

            }*/
        }
    }
}
