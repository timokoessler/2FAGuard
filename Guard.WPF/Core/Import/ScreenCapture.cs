using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace Guard.WPF.Core.Import
{
    internal class ScreenCapture
    {
        private enum SystemMetric
        {
            SM_XVIRTUALSCREEN = 76, // 0x4C
            SM_YVIRTUALSCREEN = 77, // 0x4D
            SM_CXVIRTUALSCREEN = 78, // 0x4E
            SM_CYVIRTUALSCREEN = 79, // 0x4F
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(SystemMetric smIndex);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(
            IntPtr hdcDest,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            System.Int32 dwRop
        );

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const int SRCCOPY = 0x00CC0020;

        public static SKBitmap CaptureAllScreens()
        {
            int left = GetSystemMetrics(SystemMetric.SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SystemMetric.SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SystemMetric.SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SystemMetric.SM_CYVIRTUALSCREEN);

            IntPtr hWnd = GetDesktopWindow();
            IntPtr hSrcDC = GetWindowDC(hWnd);
            IntPtr hMemDC = CreateCompatibleDC(hSrcDC);
            IntPtr hBitmap = CreateCompatibleBitmap(hSrcDC, width, height);
            IntPtr hOld = SelectObject(hMemDC, hBitmap);

            try
            {
                BitBlt(hMemDC, 0, 0, width, height, hSrcDC, left, top, SRCCOPY);

                // Create BitmapSource from HBITMAP
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );

                // Cleanup
                SelectObject(hMemDC, hOld);
                DeleteObject(hBitmap);
                DeleteDC(hMemDC);

                return WPFExtensions.ToSKBitmap(bitmapSource);
            }
            finally
            {
                SelectObject(hMemDC, hOld);
                DeleteObject(hBitmap);
                DeleteDC(hMemDC);
                ReleaseDC(hWnd, hSrcDC);
            }
        }
    }
}
