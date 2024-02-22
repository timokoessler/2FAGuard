using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Models;
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

            SystemThemeRadioButton.Checked += OnSystemThemeRadioButtonChecked;
            LightThemeRadioButton.Checked += OnLightThemeRadioButtonChecked;
            DarkThemeRadioButton.Checked += OnDarkThemeRadioButtonChecked;

            string[] supportedLanguages = Enum.GetNames(typeof(LanguageSetting));
            foreach (string lang in supportedLanguages)
            {
                LanguagesComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = I18n.GetFullLanguageName(lang),
                        Tag = lang.ToString().ToLower()
                    }
                );
            }

            SetSelectedLanguage(
                SettingsManager.Settings.Language == LanguageSetting.System
                    ? LanguageSetting.System
                    : SettingsManager.Settings.Language
            );

            LanguagesComboBox.SelectionChanged += OnLanguageSelectionChanged;
        }

        private void SetSelectedLanguage(LanguageSetting lang)
        {
            LanguagesComboBox.SelectedItem = LanguagesComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => (string)x.Tag == lang.ToString().ToLower());
        }

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguagesComboBox.SelectedItem != null)
            {
                string lang = (string)((ComboBoxItem)LanguagesComboBox.SelectedItem).Tag;

                if (Enum.TryParse<LanguageSetting>(lang, true, out LanguageSetting result))
                {
                    I18n.ChangeLanguage(result);
                }
            }
        }

        private void OnSystemThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ApplicationTheme theme =
                ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark
                    ? ApplicationTheme.Dark
                    : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica);
        }

        private void OnLightThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica);
        }

        private void OnDarkThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);
        }
    }
}
