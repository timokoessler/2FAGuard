using System.Drawing;
using System.Runtime.InteropServices;

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

        public static Bitmap CaptureAllScreens()
        {
            int screenLeft = GetSystemMetrics(SystemMetric.SM_XVIRTUALSCREEN);
            int screenTop = GetSystemMetrics(SystemMetric.SM_YVIRTUALSCREEN);
            int screenWidth = GetSystemMetrics(SystemMetric.SM_CXVIRTUALSCREEN);
            int screenHeight = GetSystemMetrics(SystemMetric.SM_CYVIRTUALSCREEN);

            Bitmap bmp = new(screenWidth, screenHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
            }

            return bmp;
        }
    }
}
