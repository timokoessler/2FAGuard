using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Dialogs;

public partial class QRDialog : ContentDialog
{
    public QRDialog(ContentPresenter contentPresenter, BitmapImage bitmap, int height, int width)
        : base(contentPresenter)
    {
        InitializeComponent();
        ImageEle.Source = bitmap;
        ImageEle.Height = height;
        ImageEle.Width = width;
    }
}
