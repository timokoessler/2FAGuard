using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Guard.Core;
using Guard.Core.Models;
using Guard.WPF.Core;
using Guard.WPF.Core.Export;
using Guard.WPF.Core.Icons;
using Guard.WPF.Views.Controls;
using Guard.WPF.Views.Pages;
using Guard.WPF.Views.Pages.Add;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.UIComponents
{
    /// <summary>
    /// Interaktionslogik für TokenCard.xaml
    /// </summary>
    partial class TokenCard : UserControl
    {
        private readonly TOTPTokenHelper token;
        private DoubleAnimation? doubleAnimation;
        private readonly MainWindow mainWindow;
        internal readonly string SearchString;
        private readonly TotpIcon? icon;

        internal TokenCard(TOTPTokenHelper token)
        {
            InitializeComponent();
            this.token = token;

            mainWindow = (MainWindow)Application.Current.MainWindow;

            if (string.IsNullOrEmpty(token.dBToken.Issuer))
            {
                token.dBToken.Issuer = "???";
            }

            Issuer.Text = token.dBToken.Issuer;
            if (token.dBToken.Issuer.Length > 18)
            {
                Issuer.ToolTip = token.dBToken.Issuer;
            }
            if (token.Username != null)
            {
                Username.Text = token.Username;
                if (token.Username.Length > 22)
                {
                    Username.ToolTip = token.Username;
                }
            }
            else
            {
                Username.Visibility = Visibility.Collapsed;
            }

            if (token.dBToken.Icon != null && token.dBToken.IconType != null)
            {
                icon = IconManager.GetIcon(
                    token.dBToken.Icon,
                    token.dBToken.IconType ?? IconType.Any
                );

                if (icon.Type != IconType.Custom)
                {
                    SvgIconView.SvgSource = icon.Svg;
                }
                else
                {
                    if (icon.Path != null && File.Exists(icon.Path))
                    {
                        if (icon.Path.EndsWith(".svg"))
                        {
                            SvgIconView.Source = new Uri(icon.Path);
                        }
                        else
                        {
                            ImageIconView.Visibility = Visibility.Visible;
                            SvgIconView.Visibility = Visibility.Collapsed;

                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(icon.Path);
                            bitmap.EndInit();
                            ImageIconView.Source = bitmap;
                        }
                    }
                    else
                    {
                        Log.Logger.Warning(
                            "Custom icon not found: {Path}",
                            icon.Path,
                            token.dBToken.Id
                        );
                        icon = IconManager.GetIcon("default", IconType.Default);
                        SvgIconView.SvgSource = icon.Svg;
                    }
                }
            }
            else
            {
                icon = IconManager.GetIcon("default", IconType.Default);
                SvgIconView.SvgSource = icon.Svg;
            }

            UpdateTokenText();
            InitProgressRing();

            SearchString = $"{token.dBToken.Issuer.ToLower()} {token.Username?.ToLower()}";

            // Add Click event to copy token to clipboard
            MouseLeftButtonUp += (sender, e) =>
            {
                CopyToken();
            };

            KeyDown += (sender, e) =>
            {
                if (IsKeyboardFocused)
                {
                    if (e.Key == Key.Enter || e.Key == Key.C || e.Key == Key.Space)
                    {
                        CopyToken();
                    }
                    else if (e.Key == Key.Q)
                    {
                        MenuItem_Qr_Click(sender, e);
                    }
                    else if (e.Key == Key.E)
                    {
                        MenuItem_Edit_Click(sender, e);
                    }
                    else if (e.Key == Key.Delete)
                    {
                        MenuItem_Delete_Click(sender, e);
                    }
                }
            };

            Cursor = Cursors.Hand;
        }

        internal void Update()
        {
            int remainingSeconds = token.GetRemainingSeconds();
            if (remainingSeconds == (token.dBToken.Period ?? 30) || TimeProgressRing.Progress == 0)
            {
                TimeProgressRing.Foreground = ApplicationAccentColorManager.PrimaryAccentBrush;
                UpdateTokenText();
                if (doubleAnimation != null)
                {
                    doubleAnimation.From = 100;
                    doubleAnimation.Duration = TimeSpan.FromSeconds(token.dBToken.Period ?? 30);
                    TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, doubleAnimation);
                }
                return;
            }
            if (remainingSeconds < 6)
            {
                TimeProgressRing.Foreground = Brushes.Red;
            }
        }

        private void InitProgressRing()
        {
            int totalSeconds = token.dBToken.Period ?? 30;
            int remainingSeconds = token.GetRemainingSeconds();

            int start = (int)((double)remainingSeconds / (double)totalSeconds * 100);

            doubleAnimation = new()
            {
                From = start,
                To = 0,
                Duration = TimeSpan.FromSeconds(remainingSeconds),
            };
            TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, doubleAnimation);
        }

        private void UpdateTokenText()
        {
            string tokenStr = token.GenerateToken();
            if (tokenStr.Length == 6)
            {
                tokenStr = tokenStr[..3] + " " + tokenStr[3..];
            }
            else if (tokenStr.Length == 8)
            {
                tokenStr = tokenStr[..4] + " " + tokenStr[4..];
            }
            TokenTextBlock.Text = tokenStr;
        }

        private async void CopyToken()
        {
            try
            {
                Clipboard.SetText(token.GenerateToken());
                TimeProgressRing.Visibility = Visibility.Collapsed;
                SvgIconRingView.Visibility = Visibility.Visible;
                await Task.Delay(1000);
                SvgIconRingView.Visibility = Visibility.Collapsed;
                TimeProgressRing.Visibility = Visibility.Visible;
            }
            catch
            {
                // Can happen if user makes really fast clicks
            }
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToken();
        }

        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            NavigationContextManager.CurrentContext["tokenID"] = token.dBToken.Id;
            NavigationContextManager.CurrentContext["action"] = "edit";
            bool showBackButton = mainWindow.GetActivePage() == "Home";
            mainWindow.Navigate(typeof(TokenSettings), showBackButton);
        }

        private async void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var deleteMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("tokencard.delete.modal.title"),
                Content = I18n.GetString("tokencard.delete.modal.content")
                    .Replace("@Name", token.dBToken.Issuer),
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = I18n.GetString("tokencard.delete.modal.yes"),
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 400
            };

            var result = await deleteMessageBox.ShowDialogAsync();
            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                TokenManager.DeleteTokenById(token.dBToken.Id);
                if (
                    token.dBToken.Icon != null
                    && token.dBToken.IconType != null
                    && token.dBToken.IconType == IconType.Custom
                )
                {
                    IconManager.RemoveCustomIcon(token.dBToken.Icon);
                }
                if (mainWindow.GetActivePage() != "Home")
                {
                    mainWindow.Navigate(typeof(Home));
                    return;
                }
                Core.EventManager.EmitTokenDeleted(token.dBToken.Id);
            }
        }

        internal string GetIssuer()
        {
            return token.dBToken.Issuer;
        }

        internal DateTime GetCreationTime()
        {
            return token.dBToken.CreationTime;
        }

        private async void MenuItem_Qr_Click(object sender, RoutedEventArgs e)
        {
            string uri = OTPUriCreator.GetUri(token);
            var qrImage = QRCode.GenerateQRCodeImage(uri, 380);
            var dialog = new QRDialog(mainWindow.GetRootContentDialogPresenter(), qrImage, 380, 380)
            {
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = I18n.GetString("tokencard.save"),
                IsSecondaryButtonEnabled = true,
                SecondaryButtonText = I18n.GetString("tokencard.copy"),
                DialogMaxHeight = 600
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG (*.png)|*.png",
                    FileName = $"{token.dBToken.Issuer}.png"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    using FileStream fileStream = new(saveFileDialog.FileName, FileMode.Create);
                    PngBitmapEncoder encoder = new();
                    encoder.Frames.Add(BitmapFrame.Create(qrImage));
                    encoder.Save(fileStream);
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                Clipboard.SetImage(qrImage);
            }
        }

        internal int GetTokenId()
        {
            return token.dBToken.Id;
        }
    }
}
