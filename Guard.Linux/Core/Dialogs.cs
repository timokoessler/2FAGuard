using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluentAvalonia.UI.Controls;
using Guard.WPF.Core;

namespace Guard.Linux.Core
{
    internal enum DialogResult
    {
        None,
        Primary,
        Secondary,
    }

    internal static class Dialogs
    {
        private static bool dialogOpen;
        private static readonly SemaphoreSlim dialogLock = new(1, 1);

        internal static bool IsDialogOpen => dialogOpen;

        private static async Task<DialogResult> Show(ContentDialog dialog)
        {
            await dialogLock.WaitAsync();
            try
            {
                dialogOpen = true;
                var result = App.MainWindow != null && App.MainWindow.IsVisible
                    ? await dialog.ShowAsync(App.MainWindow)
                    : await dialog.ShowAsync();
                return result switch
                {
                    ContentDialogResult.Primary => DialogResult.Primary,
                    ContentDialogResult.Secondary => DialogResult.Secondary,
                    _ => DialogResult.None,
                };
            }
            finally
            {
                dialogOpen = false;
                dialogLock.Release();
            }
        }

        private static Control TextContent(string text, double maxWidth)
        {
            return new ScrollViewer
            {
                MaxHeight = 420,
                Content = new SelectableTextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = maxWidth,
                },
            };
        }

        internal static Task<DialogResult> MessageBox(
            string title,
            string content,
            string? closeButtonText = null,
            string? primaryButtonText = null,
            double maxWidth = 400,
            bool primaryIsDanger = false
        )
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = TextContent(content, maxWidth),
                CloseButtonText = closeButtonText ?? I18n.GetString("dialog.close"),
                DefaultButton = primaryButtonText != null && !primaryIsDanger
                    ? ContentDialogButton.Primary
                    : ContentDialogButton.Close,
            };
            if (primaryButtonText != null)
            {
                dialog.PrimaryButtonText = primaryButtonText;
                dialog.IsPrimaryButtonEnabled = true;
                if (primaryIsDanger)
                {
                    dialog.Classes.Add("danger");
                }
            }
            return Show(dialog);
        }

        internal static Task<DialogResult> Error(string content)
        {
            return MessageBox(I18n.GetString("error"), content);
        }

        /// <summary>
        /// Asks for the app password. Returns null if the dialog was canceled.
        /// </summary>
        internal static async Task<byte[]?> PasswordDialog(string? description = null)
        {
            var box = new TextBox
            {
                PasswordChar = '●',
                Watermark = I18n.GetString("passdialog.input"),
                Margin = new Thickness(0, 15, 0, 0),
                Classes = { "revealPasswordButton" },
            };
            var dialog = new ContentDialog
            {
                Title = I18n.GetString("passdialog.title"),
                Content = new StackPanel
                {
                    Width = 340,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = description ?? I18n.GetString("passdialog.content"),
                            TextWrapping = TextWrapping.Wrap,
                        },
                        box,
                    },
                },
                PrimaryButtonText = I18n.GetString("passdialog.next"),
                IsPrimaryButtonEnabled = true,
                CloseButtonText = I18n.GetString("dialog.close"),
                DefaultButton = ContentDialogButton.Primary,
            };
            dialog.Opened += (_, _) => box.Focus();
            var result = await Show(dialog);
            if (result != DialogResult.Primary)
            {
                return null;
            }
            return Encoding.UTF8.GetBytes(box.Text ?? "");
        }

        internal static async Task<string?> TextInputDialog(
            string title,
            string content,
            string placeholder,
            string? note = null,
            bool isPassword = false
        )
        {
            var box = new TextBox { Watermark = placeholder, Margin = new Thickness(0, 15, 0, 0) };
            if (isPassword)
            {
                box.PasswordChar = '●';
                box.Classes.Add("revealPasswordButton");
            }
            var panel = new StackPanel
            {
                Width = 340,
                Children =
                {
                    new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap },
                    box,
                },
            };
            if (note != null)
            {
                panel.Children.Add(
                    new TextBlock
                    {
                        Text = note,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 15, 0, 0),
                        Opacity = 0.75,
                    }
                );
            }
            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = I18n.GetString("dialog.next"),
                IsPrimaryButtonEnabled = true,
                CloseButtonText = I18n.GetString("dialog.close"),
                DefaultButton = ContentDialogButton.Primary,
            };
            dialog.Opened += (_, _) => box.Focus();
            var result = await Show(dialog);
            return result == DialogResult.Primary ? box.Text ?? "" : null;
        }

        internal static Task<DialogResult> QRDialog(Bitmap image, int size)
        {
            var dialog = new ContentDialog
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new Image
                        {
                            Source = image,
                            Width = size,
                            Height = size,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new InfoBar
                        {
                            IsOpen = true,
                            IsClosable = false,
                            Message = I18n.GetString("tokencard.qr.keepsecure"),
                            Severity = InfoBarSeverity.Warning,
                            Margin = new Thickness(0, 10, 0, 0),
                            MaxWidth = size,
                        },
                    },
                },
                PrimaryButtonText = I18n.GetString("tokencard.save"),
                IsPrimaryButtonEnabled = true,
                SecondaryButtonText = I18n.GetString("tokencard.copy"),
                IsSecondaryButtonEnabled = true,
                CloseButtonText = I18n.GetString("dialog.close"),
            };
            return Show(dialog);
        }

        /// <summary>
        /// Lets the user choose one of multiple items. Returns the index or -1 if canceled.
        /// </summary>
        internal static async Task<int> SelectDialog(string title, IList<string> items)
        {
            var list = new ListBox
            {
                ItemsSource = items,
                SelectedIndex = 0,
                MinWidth = 320,
                MaxHeight = 300,
            };
            var dialog = new ContentDialog
            {
                Title = title,
                Content = list,
                PrimaryButtonText = I18n.GetString("dialog.next"),
                IsPrimaryButtonEnabled = true,
                CloseButtonText = I18n.GetString("dialog.close"),
                DefaultButton = ContentDialogButton.Primary,
            };
            var result = await Show(dialog);
            return result == DialogResult.Primary ? list.SelectedIndex : -1;
        }
    }
}
