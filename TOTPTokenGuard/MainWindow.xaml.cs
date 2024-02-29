using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Aptabase;
using TOTPTokenGuard.Core.Icons;
using TOTPTokenGuard.Core.Models;
using TOTPTokenGuard.Core.Security;
using TOTPTokenGuard.Core.Storage;
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
        private AptabaseClient? StatsClient;

        public MainWindow()
        {
            I18n.InitI18n();

            InitializeComponent();
            Loaded += (s, e) => OnWindowLoaded();

            RootNavigation.SelectionChanged += OnNavigationSelectionChanged;
            ContentDialogService = new ContentDialogService();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                OnUnhandledException
            );
        }

        private async void OnWindowLoaded()
        {
            ContentDialogService.SetContentPresenter(RootContentDialogPresenter);
            HideNavigation();

            await SettingsManager.Init();
            ApplyTheme(SettingsManager.Settings.Theme);
            SimpleIconsManager.LoadIcons();

            StatsClient = new(
                "A-SH-2619747927",
                new InitOptions { Host = "https://aptabase.tkoessler.de" }
            );

            if (!Auth.FileExists())
            {
                StatsClient.TrackEvent("AppSetup");
                FullContentFrame.Content = new Welcome();
                return;
            }

            StatsClient.TrackEvent("AppOpened");
            FullContentFrame.Content = new Login();
        }

        internal void ApplyTheme(Core.Models.ThemeSetting theme)
        {
            if (theme == Core.Models.ThemeSetting.System)
            {
                SystemThemeWatcher.Watch(this);
                ApplicationTheme appTheme =
                    ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark
                        ? ApplicationTheme.Dark
                        : ApplicationTheme.Light;
                if (ApplicationThemeManager.GetAppTheme() != appTheme)
                {
                    ApplicationThemeManager.Apply(appTheme, WindowBackdropType.Mica);
                }
            }
            else if (theme == Core.Models.ThemeSetting.Dark)
            {
                SystemThemeWatcher.UnWatch(this);
                if (ApplicationThemeManager.GetAppTheme() != ApplicationTheme.Dark)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);
                }
            }
            else
            {
                SystemThemeWatcher.UnWatch(this);
                if (ApplicationThemeManager.GetAppTheme() != ApplicationTheme.Light)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica);
                }
            }
        }

        private void OnNavigationSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not NavigationView)
            {
                return;
            }

            UpdatePageTitle();
        }

        internal void UpdatePageTitle()
        {
            string? pageName = RootNavigation.SelectedItem?.TargetPageType?.Name;
            if (pageName != null)
            {
                PageTitle.Text = I18n.GetString("page." + pageName.ToLower());
            }
        }

        internal void SetPageTitle(string title)
        {
            PageTitle.Text = title;
        }

        internal void Navigate(Type page)
        {
            RootNavigation.Navigate(page);
        }

        // TODO: Bug: Mica Backdrop is not visible after Theme change
        private void ToggleThemeClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            ThemeSetting targetTheme =
                ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
                    ? ThemeSetting.Light
                    : ThemeSetting.Dark;

            ApplyTheme(targetTheme);
            SettingsManager.Settings.Theme = targetTheme;
            Core.EventManager.EmitAppThemeChanged(
                targetTheme,
                Core.EventManager.AppThemeChangedEventArgs.EventSource.Navigation
            );
            _ = SettingsManager.Save();
        }

        private void NavLockClicked(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        internal void Logout()
        {
            RootNavigation.Navigate(typeof(Welcome));
            HideNavigation();
            Auth.Logout();
            FullContentFrame.Visibility = Visibility.Visible;
            FullContentFrame.Content = new Login(false);
        }

        internal ContentDialogService GetContentDialogService()
        {
            return ContentDialogService;
        }

        internal void HideNavigation()
        {
            RootNavigation.Visibility = Visibility.Collapsed;
        }

        internal void ShowNavigation()
        {
            if (!Auth.IsLoginEnabled())
            {
                NavLock.Visibility = Visibility.Collapsed;
            }
            RootNavigation.Visibility = Visibility.Visible;
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("error.unhandled.title"),
                Content = e.Message + "\n\n" + e.StackTrace,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = I18n.GetString("error.unhandled.openbug"),
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 1000
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

        internal AptabaseClient? GetStatsClient()
        {
            return StatsClient;
        }

        internal Type? GetActivePage()
        {
            return RootNavigation?.SelectedItem?.TargetPageType;
        }

        internal ContentPresenter GetRootContentDialogPresenter()
        {
            return RootContentDialogPresenter;
        }
    }
}
