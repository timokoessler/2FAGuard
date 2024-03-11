using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace Guard.Views.Controls;

public partial class ImageDialog : ContentDialog
{
    public ImageDialog(ContentPresenter contentPresenter, BitmapImage bitmap, int height, int width)
        : base(contentPresenter)
    {
        InitializeComponent();
        ImageEle.Source = bitmap;
        ImageEle.Height = height;
        ImageEle.Width = width;
    }
}
