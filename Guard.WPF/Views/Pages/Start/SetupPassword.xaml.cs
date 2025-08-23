using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Guard.Core;
using Guard.Core.Security;
using Guard.WPF.Core;
using Guard.WPF.Core.Security;

namespace Guard.WPF.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für SetupPassword.xaml
    /// </summary>
    public partial class SetupPassword : Page
    {
        private readonly bool enableWinHello;
        private readonly MainWindow mainWindow;

        public SetupPassword(bool enableWinHello)
        {
            this.enableWinHello = enableWinHello;
            InitializeComponent();
            PasswordBox.Focus();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            PasswordBox.KeyDown += (sender, e) => CapsLockWarning();
            PasswordBoxRepeat.KeyDown += (sender, e) => CapsLockWarning();

            Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;

            Unloaded += (object? sender, RoutedEventArgs e) =>
            {
                Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
            };

            OnWindowSizeChanged(null, (mainWindow.ActualWidth, mainWindow.ActualHeight));
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;

            (bool passValid, string? passInvalidReason) = PasswordComplexity.CheckPassword(
                PasswordBox.Password
            );

            if (!passValid)
            {
                ShowEror("Error", passInvalidReason ?? "Invalid Password");
                return;
            }

            if (PasswordBox.Password != PasswordBoxRepeat.Password)
            {
                ShowEror("Error", I18n.GetString("welcome.pass.notmatch"));
                return;
            }

            SaveButton.IsEnabled = false;
            RegisterProgressBar.Visibility = Visibility.Visible;

            try
            {
                await Auth.Init();
                await Auth.Register(Encoding.UTF8.GetBytes(PasswordBox.Password), enableWinHello);
                mainWindow.FullContentFrame.Content = new SetupCompleted();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error during registration: {0} {1}", ex.Message, ex.StackTrace);
                RegisterProgressBar.Visibility = Visibility.Collapsed;
                ShowEror("Error", ex.Message);
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowEror(string title, string message)
        {
            InfoBar.Title = title;
            InfoBar.Message = message;
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Error;
            InfoBar.IsOpen = true;
        }

        private void ShowWarning(string title, string message)
        {
            InfoBar.Title = title;
            InfoBar.Message = message;
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Warning;
            InfoBar.IsOpen = true;
        }

        private void CapsLockWarning()
        {
            if (Keyboard.IsKeyToggled(Key.CapsLock))
            {
                ShowWarning("", I18n.GetString("login.warning.capslock.content"));
            }
            else
            {
                if (InfoBar.Message == I18n.GetString("login.warning.capslock.content"))
                {
                    InfoBar.IsOpen = false;
                }
            }
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            if (mainWindow.ActualHeight < 600)
            {
                HeaderLogo.Visibility = Visibility.Collapsed;
                HeaderTitle.Visibility = Visibility.Collapsed;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 60;
                HeaderSubtitle.Margin = margin;
            }
            else
            {
                HeaderLogo.Visibility = Visibility.Visible;
                HeaderTitle.Visibility = Visibility.Visible;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 20;
                HeaderSubtitle.Margin = margin;
            }
        }
    }
}
