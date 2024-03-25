using System.Runtime.InteropServices;
using System.Timers;

namespace Guard.Core.Security
{
    internal class InactivityDetector
    {
        private static DateTime? lostFocusTime;
        private static bool isInactive = false;
        private static System.Timers.Timer? timer;

        public static void OnFocusLost()
        {
            lostFocusTime = DateTime.Now;
            isInactive = true;
        }

        public static void OnFocusGained()
        {
            lostFocusTime = null;
            isInactive = false;
        }

        public static void Start()
        {
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += TimerTick;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private static void TimerTick(object? sender, ElapsedEventArgs e)
        {
            // Check if the app is inactive
        }


        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;

        }

        private static TimeSpan GetLastUserInput()
        {
            var plii = new LASTINPUTINFO();
            plii.cbSize = (uint)Marshal.SizeOf(plii);
            if (GetLastInputInfo(ref plii))
                return TimeSpan.FromMilliseconds(Environment.TickCount - plii.dwTime);
            else
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);


    }
}
