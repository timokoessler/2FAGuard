using System.Windows;
using System.Windows.Controls;
using Guard.Core.Security;
using Guard.Core.Security.WebAuthn;
using Guard.WPF.Core;
using Guard.WPF.Views.Dialogs;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Pages.Preferences
{
    /// <summary>
    /// Interaktionslogik für WebAuthnPage.xaml
    /// </summary>
    public partial class WebAuthnPage : Page
    {
        private readonly MainWindow mainWindow;

        public WebAuthnPage()
        {
            InitializeComponent();

            mainWindow = (MainWindow)Application.Current.MainWindow;

            if (!WebAuthnHelper.IsSupported())
            {
                mainWindow.Navigate(typeof(Home));
                return;
            }

            var keys = Auth.GetWebAuthnDevices();
            foreach (var key in keys) { }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WebAuthnNameDialog(mainWindow.GetRootContentDialogPresenter());
            var result = await dialog.ShowAsync();

            if (!result.Equals(ContentDialogResult.Primary))
            {
                return;
            }
            string keyName = dialog.GetName();
            if (string.IsNullOrEmpty(keyName))
            {
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("webauthn.dialog1.title"),
                    Content = I18n.GetString("webauthn.dialog1.namerequired"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
                return;
            }
            try
            {
                var creationResult = await WebAuthnHelper.Register(
                    mainWindow.GetWindowHandle(),
                    keyName
                );
                if (!creationResult.success)
                {
                    if (creationResult.error == "Cancelled")
                    {
                        return;
                    }
                    _ = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("error"),
                        Content = creationResult.error,
                        CloseButtonText = I18n.GetString("dialog.close"),
                        MaxWidth = 400
                    }.ShowDialogAsync();
                    return;
                }
                // Show success dialog?
                // Todo reload list
            }
            catch (Exception ex)
            {
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("error"),
                    Content = $"Unhandled WebAuthn exception: {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
            }
        }
    }
}
