using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Security.WebAuthn;
using Guard.WPF.Core;
using Guard.WPF.Views.Dialogs;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Pages.Preferences
{
    /// <summary>
    /// Interaktionslogik für WebAuthnPage.xaml
    /// </summary>
    public partial class WebAuthnPage : Page
    {
        private readonly MainWindow mainWindow;
        private readonly EncryptionHelper encryptionHelper;
        private List<WebauthnDevice> keys = [];

        public WebAuthnPage()
        {
            InitializeComponent();

            mainWindow = (MainWindow)Application.Current.MainWindow;
            encryptionHelper = Auth.GetMainEncryptionHelper();

            if (!WebAuthnHelper.IsSupported())
            {
                mainWindow.Navigate(typeof(Home));
                return;
            }
            LoadKeys();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WebAuthnNameDialog(mainWindow.GetRootContentDialogPresenter());
            var result = await dialog.ShowAsync();

            if (!result.Equals(ContentDialogResult.Primary))
            {
                return;
            }
            string keyName = dialog.GetName();
            if (string.IsNullOrEmpty(keyName))
            {
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("webauthn.dialog1.title"),
                    Content = I18n.GetString("webauthn.dialog1.namerequired"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
                return;
            }

            // Check that the key name is unique
            foreach (var key in keys)
            {
                if (
                    !string.IsNullOrEmpty(key.EncryptedName)
                    && encryptionHelper.DecryptString(key.EncryptedName) == keyName
                )
                {
                    _ = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("webauthn.dialog1.title"),
                        Content = I18n.GetString("webauthn.dialog.nameexists"),
                        CloseButtonText = I18n.GetString("dialog.close"),
                        MaxWidth = 400
                    }.ShowDialogAsync();
                    return;
                }
            }

            try
            {
                var creationResult = await WebAuthnHelper.Register(
                    mainWindow.GetWindowHandle(),
                    keyName
                );
                if (!creationResult.success)
                {
                    if (creationResult.error == "Canceled")
                    {
                        return;
                    }
                    _ = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("error"),
                        Content = creationResult.error,
                        CloseButtonText = I18n.GetString("dialog.close"),
                        MaxWidth = 400
                    }.ShowDialogAsync();
                    return;
                }
                LoadKeys();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Unhandled WebAuthn exception on key creation: {0}", ex.Message);
                if (ex.Message == "Canceled")
                {
                    return;
                }
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("error"),
                    Content = $"Unhandled WebAuthn exception: {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
            }
        }

        private void LoadKeys()
        {
            keys = Auth.GetWebAuthnDevices();
            KeysContainer.Children.Clear();

            if (keys.Count == 0)
            {
                KeysContainer.Children.Add(
                    new Wpf.Ui.Controls.TextBlock()
                    {
                        Text = I18n.GetString("webauthn.existing.none"),
                        Margin = new Thickness(0, 15, 0, 0)
                    }
                );
                return;
            }

            foreach (var key in keys)
            {
                var delBtn = new Wpf.Ui.Controls.Button()
                {
                    Margin = new Thickness(0, 0, 8, 0),
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Delete24 },
                };
                delBtn.Click += async (sender, e) =>
                {
                    var deleteMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("webauthn.delete.title"),
                        Content = I18n.GetString("webauthn.delete.content")
                            .Replace(
                                "@Name",
                                !string.IsNullOrEmpty(key.EncryptedName)
                                    ? encryptionHelper.DecryptString(key.EncryptedName)
                                    : "???"
                            ),
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = I18n.GetString("webauthn.delete.yes"),
                        CloseButtonText = I18n.GetString("dialog.close"),
                        MaxWidth = 400
                    };

                    var result = await deleteMessageBox.ShowDialogAsync();
                    if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                    {
                        DeleteKey(key);
                    }
                };
                KeysContainer.Children.Add(
                    new CardControl()
                    {
                        Width = 320,
                        Margin = new Thickness(0, 15, 15, 0),
                        Icon = new SymbolIcon() { Symbol = SymbolRegular.UsbStick24 },
                        Header = new Grid()
                        {
                            Margin = new Thickness(0, 0, 35, 0),
                            Children =
                            {
                                new Wpf.Ui.Controls.TextBlock()
                                {
                                    Text =
                                        key.EncryptedName != null
                                            ? encryptionHelper.DecryptString(key.EncryptedName)
                                            : "???",
                                    FontTypography = FontTypography.BodyStrong,
                                    TextWrapping = TextWrapping.WrapWithOverflow
                                },
                            }
                        },
                        Content = delBtn
                    }
                );
            }
        }

        private async void DeleteKey(WebauthnDevice key)
        {
            await Auth.RemoveWebAuthnDevice(key);
            LoadKeys();
        }
    }
}
