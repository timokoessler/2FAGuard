using System.Windows;
using System.Windows.Controls;
using Guard.Core.Security.WebAuthn;
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
            var creationResult = await WebAuthnHelper.Register(
                mainWindow.GetWindowHandle(),
                keyName
            );
            // Todo add dialogs
        }
    }
}
