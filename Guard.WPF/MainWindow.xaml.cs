using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Guard.WPF.Core;
using Guard.WPF.Core.Aptabase;
using Guard.WPF.Core.Icons;
using Guard.WPF.Core.Models;
using Guard.Core.Security;
using Guard.WPF.Core.Storage;
using Guard.WPF.Views.Pages.Start;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Input;
using Wpf.Ui.Tray.Controls;
using Guard.Core;
using Guard.WPF.Core.Security;

namespace Guard.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private readonly ContentDialogService ContentDialogService;
        private AptabaseClient? StatsClient;
        private IntPtr windowInteropHandle;
        private readonly bool isAutostart;
        private string currentPageName = "";

        public MainWindow(bool autostart)
        {
            if (autostart)
            {
                isAutostart = true;
                WindowState = WindowState.Minimized;
            }

            InitializeComponent();
            Loaded += (s, e) => OnWindowLoaded();

            RootNavigation.Navigated += OnNavigated;
            ContentDialogService = new ContentDialogService();
            RootNavigation.Navigating += (s, e) =>
                RootNavigation.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                OnUnhandledException
            );

            SessionSwitchEvent.Register(this);
        }

        [DllImport("user32.dll")]
        internal static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        private void OnWindowLoaded()
        {
            ContentDialogService.SetDialogHost(RootContentDialogPresenter);
            HideNavigation();

            windowInteropHandle = (new WindowInteropHelper(this)).Handle;

            ApplyTheme(SettingsManager.Settings.Theme);

            if (SettingsManager.Settings.PreventRecording)
            {
                AllowScreenRecording(!SettingsManager.Settings.PreventRecording);
            }

            SimpleIconsManager.LoadIcons();

            StatsClient = new(
                "A-SH-2619747927",
                new AptabaseOptions { Host = "https://aptabase.tkoessler.de" }
            );

            if (!Auth.FileExists())
            {
                StatsClient.TrackEvent($"AppSetup{InstallationContext.GetInstallationTypeString()}");
                FullContentFrame.Content = new Welcome();
            }
            else
            {
                StatsClient.TrackEvent("AppOpened");
                FullContentFrame.Content = new Login(!isAutostart);
            }

            CheckLocalTime();

            if (SettingsManager.Settings.MinimizeToTray)
            {
                AddTrayIcon();
            }
        }

        internal void ApplyTheme(ThemeSetting theme)
        {
            if (theme == ThemeSetting.System)
            {
                SystemThemeWatcher.Watch(this);
                ApplicationTheme appTheme =
                    ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark
                        ? ApplicationTheme.Dark
                        : ApplicationTheme.Light;
                if (ApplicationThemeManager.GetAppTheme() != appTheme)
                {
                    ApplicationThemeManager.Apply(appTheme);
                }
            }
            else if (theme == ThemeSetting.Dark)
            {
                SystemThemeWatcher.UnWatch(this);
                if (ApplicationThemeManager.GetAppTheme() != ApplicationTheme.Dark)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                }
            }
            else
            {
                SystemThemeWatcher.UnWatch(this);
                if (ApplicationThemeManager.GetAppTheme() != ApplicationTheme.Light)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                }
            }
            this.WindowBackdropType = WindowBackdropType.Mica;
            if (Environment.OSVersion.Version.Build > 22621)
            {
                WindowBackdrop.RemoveBackground(this);
            }
        }

        private void OnNavigated(object sender, NavigatedEventArgs e)
        {
            if (sender is not NavigationView)
            {
                return;
            }
            currentPageName = e.Page.GetType().Name;
            UpdatePageTitle(currentPageName);
        }

        /// <summary>
        /// Set the title of the current page to the localized string of the given page name
        /// </summary>
        /// <param name="pageName"></param>
        internal void UpdatePageTitle(string pageName)
        {
            PageTitle.Text = I18n.GetString("page." + pageName.ToLower());
        }

        /// <summary>
        /// Set the title of the current page to the given string
        /// </summary>
        /// <param name="title"></param>
        internal void SetPageTitle(string title)
        {
            PageTitle.Text = title;
        }

        internal void Navigate(Type page, bool enableBackButton = false)
        {
            RootNavigation.Navigate(page);
            RootNavigation.IsBackButtonVisible = enableBackButton
                ? NavigationViewBackButtonVisible.Visible
                : NavigationViewBackButtonVisible.Collapsed;
        }

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
            Navigate(typeof(Welcome));
            HideNavigation();
            Auth.Logout();
            TokenManager.ClearTokens();
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
            string content = $"{I18n.GetString("error.unhandled.content")}\n\n{e.Message}";
            if (e.StackTrace != null)
            {
                content += e.StackTrace[..450] + "...";
            }
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("error.unhandled.title"),
                Content = content,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = I18n.GetString("error.unhandled.openbug"),
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 600
            };

            Log.Logger.Error("Unhandled Exception {Exception}", e);
            var result = await uiMessageBox.ShowDialogAsync();
            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                string windowsVersion = HttpUtility.UrlEncode(SystemInfo.GetOsVersion());
                string errorMessage = HttpUtility.UrlEncode(e.Message + "\n" + e.StackTrace);
                Process.Start(
                    new ProcessStartInfo(
                        $"https://github.com/timokoessler/totp-token-guard/issues/new?template=bug.yml&title=%5BBug%5D%3A+&error-message={errorMessage}&win-version={windowsVersion}&app-version={InstallationContext.GetVersionString()} ({InstallationContext.GetInstallationTypeString()})"
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

        internal string GetActivePage()
        {
            return currentPageName;
        }

        internal ContentPresenter GetRootContentDialogPresenter()
        {
            return RootContentDialogPresenter;
        }

        internal void AllowScreenRecording(bool allow)
        {
            if (allow)
            {
                _ = SetWindowDisplayAffinity(windowInteropHandle, 0);
            }
            else
            {
                _ = SetWindowDisplayAffinity(windowInteropHandle, 1);
            }
        }

        private static async void CheckLocalTime()
        {
            TimeSpan offset = await Time.GetLocalUTCTimeOffset();
            if (offset.TotalSeconds > 10 || offset.TotalSeconds < -10)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("error.time.title"),
                    Content = I18n.GetString("error.time.content"),
                    MaxWidth = 400,
                    CloseButtonText = I18n.GetString("dialog.close"),
                };

                _ = await uiMessageBox.ShowDialogAsync();
            }
        }

        internal void AddTrayIcon()
        {
            BitmapImage trayIconImage = new();
            trayIconImage.BeginInit();
            trayIconImage.UriSource = new("pack://application:,,,/Assets/logo-tray.png");
            trayIconImage.EndInit();

            NotifyIcon trayIcon =
                new()
                {
                    FocusOnLeftClick = false,
                    Icon = trayIconImage,
                    ToolTip = "2FAGuard",
                    MenuOnRightClick = true,
                    Menu = new ContextMenu
                    {
                        Items =
                        {
                            new Wpf.Ui.Controls.MenuItem
                            {
                                Header = I18n.GetString("i.tray.exit"),
                                Command = new RelayCommand<object>(Tray_Exit_Click)
                            }
                        }
                    }
                };
            trayIcon.LeftClick += Tray_Open_Click;
            if (Auth.IsLoginEnabled())
            {
                trayIcon.Menu.Items.Insert(
                    0,
                    new Wpf.Ui.Controls.MenuItem
                    {
                        Header = I18n.GetString("i.tray.lock"),
                        Command = new RelayCommand<object>(Tray_Lock_Click)
                    }
                );
            }
            RootGrid.Children.Add(trayIcon);
        }

        internal void RemoveTrayIcon()
        {
            NotifyIcon? trayIcon = RootGrid.Children.OfType<NotifyIcon>().FirstOrDefault();
            if (trayIcon != null)
            {
                RootGrid.Children.Remove(trayIcon);
                trayIcon.Unregister();
                trayIcon.Dispose();
            }
        }

        private void Tray_Exit_Click(object? sender)
        {
            Application.Current.Shutdown();
        }

        private void Tray_Lock_Click(object? sender)
        {
            RemoveTrayIcon();
            Logout();
        }

        private void Tray_Open_Click([NotNull] NotifyIcon sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (SettingsManager.Settings.MinimizeToTray)
            {
                if (WindowState == WindowState.Minimized)
                {
                    Hide();
                }
            }

            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (SettingsManager.Settings.MinimizeToTray)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                Hide();
            }
            base.OnClosing(e);
        }

        internal void MinimizeWindow()
        {
            WindowStyle tempStyle = WindowStyle;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Minimized;
            WindowStyle = tempStyle;
        }

        internal void RestoreWindow()
        {
            WindowState = WindowState.Normal;
        }
    }
}
