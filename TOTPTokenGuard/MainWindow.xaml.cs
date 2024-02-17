using System.Windows;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Views.Pages;
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
            SystemThemeWatcher.Watch(this);
            InitializeComponent();
            Loaded += (s, e) => onWindowLoaded();
            RootNavigation.SelectionChanged += OnNavigationSelectionChanged;
        }

        private void onWindowLoaded()
        {
            contentDialogService = new ContentDialogService();
            contentDialogService.SetContentPresenter(RootContentDialogPresenter);
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
    }
}
