using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Dialogs;

public partial class QRDialog : ContentDialog
{
    public QRDialog(ContentDialogHost contentDialogHost, BitmapImage bitmap, int height, int width)
        : base(contentDialogHost)
    {
        InitializeComponent();
        ImageEle.Source = bitmap;
        ImageEle.Height = height;
        ImageEle.Width = width;
    }
}
