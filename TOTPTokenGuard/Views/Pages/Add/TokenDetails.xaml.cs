using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core.Icons;
using Wpf.Ui.Controls;

namespace TOTPTokenGuard.Views.Pages.Add
{
    /// <summary>
    /// Interaktionslogik für TokenDetails.xaml
    /// </summary>
    public partial class TokenDetails : Page
    {
        public TokenDetails()
        {
            InitializeComponent();
            IconSvgView.SvgSource = IconManager.GetIcon("default", IconManager.IconColor.Colored);
            Issuer.OriginalItemsSource = IconManager.GetIconNames();

            foreach (string algorithm in new[] { "SHA1", "SHA256", "SHA512" })
            {
                AlgorithmComboBox.Items.Add(
                    new ComboBoxItem { Content = algorithm, Tag = algorithm }
                );
            }
            AlgorithmComboBox.SelectedItem = AlgorithmComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => (string)x.Tag == "SHA1");

            Issuer.SuggestionChosen += AutoSuggestBoxOnSuggestionChosen;
            Issuer.TextChanged += AutoSuggestBoxOnTextChanged;
        }

        private void AutoSuggestBoxOnSuggestionChosen(
            AutoSuggestBox sender,
            AutoSuggestBoxSuggestionChosenEventArgs args
        )
        {
            if (sender.IsSuggestionListOpen)
                return;

            if (args.SelectedItem is not string selectedSuggestBoxItem)
                return;

            IconSvgView.SvgSource = IconManager.GetIcon(
                selectedSuggestBoxItem,
                IconManager.IconColor.Colored
            );
            ImageLicense.Text = SimpleIconsManager.GetLicense();
            NoIconText.Visibility = Visibility.Collapsed;
        }

        private void AutoSuggestBoxOnTextChanged(
            AutoSuggestBox sender,
            AutoSuggestBoxTextChangedEventArgs args
        )
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                IconSvgView.SvgSource = IconManager.GetIcon(
                    "default",
                    IconManager.IconColor.Colored
                );
                ImageLicense.Text = string.Empty;
                NoIconText.Visibility = Visibility.Visible;
            }
        }
    }
}
