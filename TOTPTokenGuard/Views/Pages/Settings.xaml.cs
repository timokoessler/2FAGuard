using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TOTPTokenGuard.Core;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace TOTPTokenGuard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();

            AppInfoTextBlock.Text =
                $"Copyright © {DateTime.Now.Year} Timo Kössler and Open Source Contributors\n";
            AppInfoTextBlock.Text += $"Version {Utils.GetVersionString()}";
            if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
            {
                DarkThemeRadioButton.IsChecked = true;
            }
            else
            {
                LightThemeRadioButton.IsChecked = true;
            }

            LightThemeRadioButton.Checked += OnLightThemeRadioButtonChecked;
            DarkThemeRadioButton.Checked += OnDarkThemeRadioButtonChecked;

            ApplicationThemeManager.Changed += OnApplicationThemeChanged;

            string[] supportedLanguages = I18n.GetSupportedLanguages();
            foreach (string lang in supportedLanguages)
            {
                LanguagesComboBox.Items.Add(
                    new ComboBoxItem { Content = I18n.GetFullLanguageName(lang), Tag = lang }
                );
            }
            SetSelectedLanguage(I18n.GetCurrentLanguage());

            LanguagesComboBox.SelectionChanged += OnLanguageSelectionChanged;
        }

        private void SetSelectedLanguage(string lang)
        {
            LanguagesComboBox.SelectedItem = LanguagesComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => (string)x.Tag == lang);
        }

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguagesComboBox.SelectedItem != null)
            {
                string lang = (string)((ComboBoxItem)LanguagesComboBox.SelectedItem).Tag;
                I18n.ChangeLanguage(lang);
            }
        }

        private void OnLightThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica);
        }

        private void OnDarkThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);
        }

        private void OnApplicationThemeChanged(
            ApplicationTheme currentApplicationTheme,
            Color systemAccent
        )
        {
            if (currentApplicationTheme == ApplicationTheme.Dark)
            {
                DarkThemeRadioButton.IsChecked = true;
            }
            else
            {
                LightThemeRadioButton.IsChecked = true;
            }
        }
    }
}
