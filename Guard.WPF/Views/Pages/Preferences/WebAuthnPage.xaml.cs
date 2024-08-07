using System.Windows;
using System.Windows.Controls;
using Guard.Core.Security.WebAuthn;

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

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            //WebAuthnHelper.Register(mainWindow.GetWindowHandle());
        }
    }
}
