using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.Linux.Core;
using Guard.Linux.Core.Security;
using Guard.Linux.Views.Pages;
using Guard.Linux.Views.Pages.Add;
using Guard.Linux.Views.Pages.Start;
using Guard.WPF.Core;
using Guard.WPF.Core.Icons;

namespace Guard.Linux.Views
{
    public partial class MainWindow : Window
    {
        private readonly bool isAutostart;
        private string currentPageName = "";
        private readonly Stack<Control> backStack = new();
        private TrayIcon? trayIcon;
        internal bool SkipApplyRegistrySettings = false;

        public MainWindow()
            : this(false) { }

        public MainWindow(bool autostart)
        {
            isAutostart = autostart;
            InitializeComponent();

            Activated += (_, _) => InactivityDetector.OnFocusGained();
            Deactivated += (_, _) => InactivityDetector.OnFocusLost();
            AddHandler(KeyDownEvent, (_, _) => InactivityDetector.OnUserInput(), RoutingStrategies.Tunnel, true);
            AddHandler(PointerPressedEvent, (_, _) => InactivityDetector.OnUserInput(), RoutingStrategies.Tunnel, true);
            AddHandler(PointerMovedEvent, (_, _) => InactivityDetector.OnUserInput(), RoutingStrategies.Tunnel, true);
            AddHandler(PointerWheelChangedEvent, (_, _) => InactivityDetector.OnUserInput(), RoutingStrategies.Tunnel, true);
        }

        /// <summary>
        /// Called by the app after all checks passed
        /// </summary>
        internal void Start()
        {
            ApplyTheme(SettingsManager.Settings.Theme);
            Application.Current!.ActualThemeVariantChanged += (_, _) =>
            {
                if (SettingsManager.Settings.Theme == ThemeSetting.System)
                {
                    WPF.Core.EventManager.EmitAppThemeChanged(
                        ThemeSetting.System,
                        WPF.Core.EventManager.AppThemeChangedEventArgs.EventSource.Navigation
                    );
                }
            };

            if (SettingsManager.Settings.RestoreWindowPlacement)
            {
                WindowPlacement.ApplyPlacement(this);
            }

            InactivityDetector.Init(Logout);
            ScreenLockMonitor.Register(Logout);
            IPC.InitPipeServer(BringToFront);

            SimpleIconsManager.LoadIcons();

            if (SettingsManager.Settings.MinimizeToTray)
            {
                AddTrayIcon();
            }

            HideNavigation();
            if (!Auth.FileExists())
            {
                ShowFullContent(new Welcome());
            }
            else
            {
                ShowFullContent(new Login(!isAutostart));
            }

            SizeChanged += (_, e) => OnSizeChanged(e.NewSize);

            if (isAutostart)
            {
                if (SettingsManager.Settings.MinimizeToTray)
                {
                    // Stay in the tray
                }
                else
                {
                    Show();
                    WindowState = WindowState.Minimized;
                }
            }
            else
            {
                Show();
            }

            CheckLocalTime();
        }

        internal void BringToFront()
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            Topmost = true;
            Topmost = false;

            if (SettingsManager.Settings.MinimizeToTray)
            {
                RemoveTrayIcon();
                AddTrayIcon();
            }
        }

        internal static void ApplyTheme(ThemeSetting theme)
        {
            Application.Current!.RequestedThemeVariant = theme switch
            {
                ThemeSetting.Light => ThemeVariant.Light,
                ThemeSetting.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }

        private void ToggleTheme_Click(object? sender, RoutedEventArgs e)
        {
            ThemeSetting targetTheme = IconColor.IsDarkMode() ? ThemeSetting.Light : ThemeSetting.Dark;

            ApplyTheme(targetTheme);
            SettingsManager.Settings.Theme = targetTheme;
            WPF.Core.EventManager.EmitAppThemeChanged(
                targetTheme,
                WPF.Core.EventManager.AppThemeChangedEventArgs.EventSource.Navigation
            );
            _ = SettingsManager.Save();
        }

        #region Navigation

        internal void Navigate(Type pageType, bool enableBackButton = false)
        {
            Control page = (Control)Activator.CreateInstance(pageType)!;
            Navigate(page, enableBackButton);
        }

        internal void Navigate(Control page, bool enableBackButton = false)
        {
            if (!enableBackButton)
            {
                backStack.Clear();
            }
            else if (PageHost.Content is Control current)
            {
                backStack.Push(current);
            }
            SetPage(page);
        }

        private void SetPage(Control page)
        {
            PageHost.Content = page;
            PageScrollViewer.Offset = default;
            BackButton.IsVisible = backStack.Count > 0;
            currentPageName = page.GetType().Name;
            UpdatePageTitle(currentPageName.ToLowerInvariant());
            UpdateActiveNavItem();
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            GoBack();
        }

        internal void GoBack()
        {
            if (backStack.Count == 0)
            {
                return;
            }
            SetPage(backStack.Pop());
        }

        private void UpdateActiveNavItem()
        {
            HomeNavigationItem.Classes.Set("active", currentPageName == nameof(Home));
            AddNavigationItem.Classes.Set("active", currentPageName == nameof(AddOverview));
            ExportNavigationItem.Classes.Set("active", currentPageName == nameof(ExportPage));
            SettingsNavigationItem.Classes.Set("active", currentPageName == nameof(Settings));
        }

        private void NavHome_Click(object? sender, RoutedEventArgs e) => Navigate(typeof(Home));

        private void NavAdd_Click(object? sender, RoutedEventArgs e) => Navigate(typeof(AddOverview));

        private void NavExport_Click(object? sender, RoutedEventArgs e) => Navigate(typeof(ExportPage));

        private void NavSettings_Click(object? sender, RoutedEventArgs e) => Navigate(typeof(Settings));

        private void NavLock_Click(object? sender, RoutedEventArgs e) => Logout();

        /// <summary>
        /// Set the title of the current page to the localized string of the given page name
        /// </summary>
        internal void UpdatePageTitle(string pageName)
        {
            string key = "i.page." + pageName;
            PageTitle.Text = Application.Current!.TryGetResource(key, null, out object? value)
                ? value as string
                : string.Empty;
        }

        internal void SetPageTitle(string title)
        {
            PageTitle.Text = title;
        }

        internal string GetActivePage()
        {
            return currentPageName;
        }

        internal void ShowFullContent(Control content)
        {
            FullContentHost.Content = content;
            FullContentFrame.Offset = default;
            FullContentFrame.IsVisible = true;
        }

        internal void ClearFullContentFrame()
        {
            FullContentHost.Content = null;
            FullContentFrame.IsVisible = false;
        }

        internal void HideNavigation()
        {
            RootNavigation.IsVisible = false;
        }

        internal void ShowNavigation()
        {
            NavLock.IsVisible = Auth.IsLoginEnabled();
            RootNavigation.IsVisible = true;
        }

        internal void Logout()
        {
            SkipApplyRegistrySettings = true;
            backStack.Clear();
            PageHost.Content = null;
            currentPageName = "";
            HideNavigation();
            InactivityDetector.Stop();
            Auth.Logout();
            TokenManager.ClearTokens();
            ShowFullContent(new Login(false));
        }

        #endregion

        private static async void CheckLocalTime()
        {
            try
            {
                TimeSpan offset = await Time.GetLocalUTCTimeOffset();
                if (offset.TotalSeconds > 10 || offset.TotalSeconds < -10)
                {
                    await Dialogs.MessageBox(
                        I18n.GetString("error.time.title"),
                        I18n.GetString("error.time.content")
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Failed to check local time: {0}", ex.Message);
            }
        }

        #region Tray

        internal void AddTrayIcon()
        {
            if (trayIcon != null)
            {
                return;
            }
            using var stream = AssetLoader.Open(new Uri("avares://2fa-guard/Assets/logo-tray.png"));
            var menu = new NativeMenu();
            var openItem = new NativeMenuItem("2FAGuard");
            openItem.Click += (_, _) => BringToFront();
            menu.Items.Add(openItem);
            if (Auth.IsLoginEnabled())
            {
                var lockItem = new NativeMenuItem(I18n.GetString("i.tray.lock"));
                lockItem.Click += (_, _) =>
                {
                    if (Auth.IsLoggedIn())
                    {
                        Logout();
                    }
                };
                menu.Items.Add(lockItem);
            }
            var exitItem = new NativeMenuItem(I18n.GetString("i.tray.exit"));
            exitItem.Click += (_, _) => App.Instance.Quit();
            menu.Items.Add(exitItem);

            trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(new Bitmap(stream)),
                ToolTipText = "2FAGuard",
                Menu = menu,
                IsVisible = true,
            };
            trayIcon.Clicked += (_, _) => BringToFront();
            TrayIcon.SetIcons(Application.Current!, [trayIcon]);
        }

        internal void RemoveTrayIcon()
        {
            if (trayIcon == null)
            {
                return;
            }
            trayIcon.IsVisible = false;
            TrayIcon.SetIcons(Application.Current!, []);
            trayIcon.Dispose();
            trayIcon = null;
        }

        #endregion

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (
                change.Property == WindowStateProperty
                && WindowState == WindowState.Minimized
                && SettingsManager.Settings.MinimizeToTray
                && trayIcon != null
            )
            {
                Dispatcher.UIThread.Post(Hide);
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (App.Instance.IsQuitting || e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
            {
                base.OnClosing(e);
                return;
            }

            if (SettingsManager.Settings.RestoreWindowPlacement)
            {
                WindowPlacement.SavePlacement(this);
            }

            e.Cancel = true;
            if (SettingsManager.Settings.MinimizeToTray && trayIcon != null)
            {
                Hide();
                return;
            }
            App.Instance.Quit();
        }

        private void OnSizeChanged(Size size)
        {
            const double HideNavElementTitlesThreshold = 800;
            const double HidePageTitleThreshold = 600;

            bool showNavTitles = size.Width >= HideNavElementTitlesThreshold;
            HomeNavigationText.IsVisible = showNavTitles;
            AddNavigationText.IsVisible = showNavTitles;
            SettingsNavigationText.IsVisible = showNavTitles;
            PageTitle.IsVisible = size.Width >= HidePageTitleThreshold;

            WPF.Core.EventManager.EmitWindowSizeChanged(size.Width, size.Height);
        }
    }
}
