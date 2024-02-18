using System.Collections;
using System.Windows;
using TOTPTokenGuard.Core;
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
        private ContentDialogService contentDialogService;

        public MainWindow()
        {
            I18n.InitI18n();
            Auth.Init();
            SystemThemeWatcher.Watch(this);
            InitializeComponent();
            Loaded += (s, e) => onWindowLoaded();
            RootNavigation.SelectionChanged += OnNavigationSelectionChanged;
            contentDialogService = new ContentDialogService();
        }

        private void onWindowLoaded()
        {
            contentDialogService.SetContentPresenter(RootContentDialogPresenter);

            if (!Auth.FileExists())
            {
                HideNavigation();
                FullContentFrame.Content = new Welcome();
                return;
            }
            RootNavigation.Navigate(typeof(Home));
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
            return contentDialogService;
        }

        public void HideNavigation()
        {
            RootNavigation.Visibility = Visibility.Collapsed;
        }

        public void ShowNavigation()
        {
            RootNavigation.Visibility = Visibility.Visible;
        }
    }
}
