using System.Windows.Controls;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Dialogs;

public partial class QRDialog : ContentDialog
{
    private SKBitmap bitmap;

    public QRDialog(ContentPresenter contentPresenter, SKBitmap bitmap, int height, int width)
        : base(contentPresenter)
    {
        InitializeComponent();
        ImageEle.PaintSurface += OnPaintSurface;
        ImageEle.Width = width;
        ImageEle.Height = height;
        this.bitmap = bitmap;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        if (bitmap != null)
        {
            int canvasWidth = e.Info.Width;
            int canvasHeight = e.Info.Height;
            int bmpWidth = bitmap.Width;
            int bmpHeight = bitmap.Height;

            float scale = Math.Min((float)canvasWidth / bmpWidth, (float)canvasHeight / bmpHeight);

            float destWidth = bmpWidth * scale;
            float destHeight = bmpHeight * scale;

            float left = (canvasWidth - destWidth) / 2f;
            float top = (canvasHeight - destHeight) / 2f;

            var destRect = new SKRect(left, top, left + destWidth, top + destHeight);
            canvas.DrawBitmap(bitmap, destRect);
        }
    }
}
