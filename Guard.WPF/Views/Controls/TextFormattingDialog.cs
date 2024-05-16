using System.Windows;
using Guard.WPF.Core;
using Wpf.Ui;
using Wpf.Ui.Extensions;

namespace Guard.WPF.Views.UIComponents
{
    internal class TextFormattingDialog
    {
        public static async void Show()
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            await mainWindow
                .GetContentDialogService()
                .ShowSimpleDialogAsync(
                    new SimpleContentDialogCreateOptions()
                    {
                        Title = I18n.GetString("txtfd.title"),
                        Content = I18n.GetString("txtfd.content").Replace("@n", "\n"),
                        CloseButtonText = I18n.GetString("dialog.close")
                    }
                );
        }
    }
}
