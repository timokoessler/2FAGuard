using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Security.Linux;
using Guard.Linux.Core;
using Guard.Linux.Core.Security;
using Guard.WPF.Core;

namespace Guard.Linux.Views.Pages.Preferences
{
    public partial class WebAuthnPage : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly EncryptionHelper encryptionHelper;
        private List<WebauthnDevice> keys = [];
        private bool busy;

        public WebAuthnPage()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;
            encryptionHelper = Auth.GetMainEncryptionHelper();

            if (!Fido2.IsSupported())
            {
                Dispatcher.UIThread.Post(() => mainWindow.Navigate(typeof(Home)));
                return;
            }
            LoadKeys();
        }

        private string GetKeyName(WebauthnDevice key) =>
            !string.IsNullOrEmpty(key.EncryptedName)
                ? encryptionHelper.DecryptString(key.EncryptedName) ?? "???"
                : "???";

        private async void Add_Click(object? sender, RoutedEventArgs e)
        {
            if (busy)
            {
                return;
            }

            string? keyName = await Dialogs.TextInputDialog(
                I18n.GetString("webauthn.dialog1.title"),
                I18n.GetString("webauthn.dialog1.content"),
                I18n.GetString("webauthn.dialog1.input"),
                I18n.GetString("webauthn.dialog1.content2")
            );
            if (keyName == null)
            {
                return;
            }
            keyName = keyName.Trim();
            if (keyName.Length == 0)
            {
                await Dialogs.MessageBox(
                    I18n.GetString("webauthn.dialog1.title"),
                    I18n.GetString("webauthn.dialog1.namerequired")
                );
                return;
            }

            if (keys.Any(k => GetKeyName(k) == keyName))
            {
                await Dialogs.MessageBox(
                    I18n.GetString("webauthn.dialog1.title"),
                    I18n.GetString("webauthn.dialog.nameexists")
                );
                return;
            }

            busy = true;
            AddCard.IsEnabled = false;
            try
            {
                var (device, pin) = await SecurityKeyFlow.PrepareDevice();
                ProgressBar.IsVisible = true;
                InfoBar.Title = "";
                InfoBar.Message = I18n.GetString("linux.fido2.touch");
                InfoBar.Severity = InfoBarSeverity.Informational;
                InfoBar.IsOpen = true;

                await Auth.RegisterSecurityKey(device, pin, keyName);
                LoadKeys();
            }
            catch (Exception ex)
            {
                if (!SecurityKeyFlow.IsCanceled(ex))
                {
                    Log.Logger.Error("Security key registration failed: {0}", ex.Message);
                    string message = ex is Fido2Exception { IsPinInvalid: true }
                        ? I18n.GetString("passdialog.incorrect")
                        : ex.Message;
                    await Dialogs.MessageBox(I18n.GetString("error"), message);
                }
            }
            finally
            {
                ProgressBar.IsVisible = false;
                InfoBar.IsOpen = false;
                AddCard.IsEnabled = true;
                busy = false;
            }
        }

        private void LoadKeys()
        {
            keys = Auth.GetWebAuthnDevices();
            KeysContainer.Children.Clear();

            if (keys.Count == 0)
            {
                KeysContainer.Children.Add(
                    new TextBlock
                    {
                        Text = I18n.GetString("webauthn.existing.none"),
                        Margin = new Thickness(0, 15, 0, 0),
                    }
                );
                return;
            }

            foreach (var key in keys)
            {
                string name = GetKeyName(key);
                var deleteButton = new Button
                {
                    Content = new PathIcon
                    {
                        Width = 16,
                        Height = 16,
                        Data = (Geometry)Application.Current!.FindResource("Icon.Delete")!,
                    },
                };
                ToolTip.SetTip(deleteButton, I18n.GetString("webauthn.delete.yes"));
                deleteButton.Click += async (_, _) =>
                {
                    var result = await Dialogs.MessageBox(
                        I18n.GetString("webauthn.delete.title"),
                        I18n.GetString("webauthn.delete.content").Replace("@Name", name),
                        primaryButtonText: I18n.GetString("webauthn.delete.yes"),
                        primaryIsDanger: true
                    );
                    if (result == DialogResult.Primary)
                    {
                        await Auth.RemoveWebAuthnDevice(key);
                        LoadKeys();
                    }
                };

                KeysContainer.Children.Add(
                    new SettingsExpander
                    {
                        Width = 320,
                        Margin = new Thickness(0, 15, 15, 0),
                        Header = name,
                        IconSource = new PathIconSource
                        {
                            Data = (Geometry)Application.Current!.FindResource("Icon.UsbStick")!,
                        },
                        Footer = deleteButton,
                    }
                );
            }
        }
    }
}
