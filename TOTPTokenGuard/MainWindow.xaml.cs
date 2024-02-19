using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Web;
using System.Windows;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Aptabase;
using TOTPTokenGuard.Core.Security;
using TOTPTokenGuard.Views.Pages;
using TOTPTokenGuard.Views.Pages.Start;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace TOTPTokenGuard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private readonly ContentDialogService ContentDialogService;
        private readonly AptabaseClient StatsClient;

        public MainWindow()
        {
            I18n.InitI18n();
            SystemThemeWatcher.Watch(this);
            InitializeComponent();
            Loaded += (s, e) => OnWindowLoaded();
            RootNavigation.SelectionChanged += OnNavigationSelectionChanged;
            ContentDialogService = new ContentDialogService();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                OnUnhandledException
            );

            StatsClient = new(
                "A-SH-2619747927",
                new InitOptions { Host = "https://aptabase.tkoessler.de" }
            );
        }

        private void OnWindowLoaded()
        {
            ContentDialogService.SetContentPresenter(RootContentDialogPresenter);
            HideNavigation();
            if (!Auth.FileExists())
            {
                FullContentFrame.Content = new Welcome();
                return;
            }

            FullContentFrame.Content = new Login();

            StatsClient.TrackEvent("AppOpened");
        }

        private void OnNavigationSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not Wpf.Ui.Controls.NavigationView navigationView)
            {
                return;
            }

            string? pageName = navigationView.SelectedItem?.TargetPageType?.Name;
            if (pageName != null)
            {
                PageTitle.Text = I18n.GetString("page." + pageName.ToLower());
            }
        }

        public void Navigate(Type page)
        {
            RootNavigation.Navigate(page);
        }

        // TODO: Bug: Mica Backdrop is not visible after Theme change
        private void ToggleThemeClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica);
            }
            else
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);
            }
        }

        public ContentDialogService GetContentDialogService()
        {
            return ContentDialogService;
        }

        public void HideNavigation()
        {
            RootNavigation.Visibility = Visibility.Collapsed;
        }

        public void ShowNavigation()
        {
            RootNavigation.Visibility = Visibility.Visible;
        }

        public async void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Unhandled Exception",
                Content = e.Message + "\n\n" + e.StackTrace,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "Open Bug Report",
            };

            var result = await uiMessageBox.ShowDialogAsync();
            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                string windowsVersion = HttpUtility.UrlEncode(SystemInfo.GetOsVersion());
                string errorMessage = HttpUtility.UrlEncode(e.Message + "\n\n" + e.StackTrace);
                Process.Start(
                    new ProcessStartInfo(
                        $"https://github.com/timokoessler/totp-token-guard/issues/new?template=bug.yml&title=%5BBug%5D%3A+&error-message={errorMessage}&win-version={windowsVersion}&app-version={Utils.GetVersionString()}"
                    )
                    {
                        UseShellExecute = true
                    }
                );
            }
            Environment.Exit(1);
        }
    }
}
