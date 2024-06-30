using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using Guard.Core.Models;
using Guard.Core.Storage;

namespace Guard.WPF.Core.Security
{
    internal class InactivityDetector
    {
        private static DateTime? lostFocusTime;
        private static System.Timers.Timer? timer;
        private static MainWindow? mainWindow;

        public static void OnFocusLost()
        {
            lostFocusTime = DateTime.Now;
        }

        public static void OnFocusGained()
        {
            lostFocusTime = null;
        }

        public static void Start()
        {
            if (SettingsManager.Settings.LockTime == LockTimeSetting.Never)
            {
                return;
            }
            mainWindow ??= (MainWindow)Application.Current.MainWindow;
            timer = new System.Timers.Timer(3000);
            timer.Elapsed += TimerTick;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public static void Stop()
        {
            timer?.Stop();
            timer?.Dispose();
        }

        public static bool IsRunning()
        {
            return timer?.Enabled ?? false;
        }

        private static void TimerTick(object? sender, ElapsedEventArgs e)
        {
            TimeSpan? timeUntilLock = GetTimeUntilLock();
            if (timeUntilLock == null)
            {
                return;
            }
            TimeSpan lastUserInput = GetLastUserInput();
            if (
                lastUserInput > timeUntilLock.Value
                || (
                    lostFocusTime != null
                    && lostFocusTime.Value.Add(timeUntilLock.Value) < DateTime.Now
                )
            )
            {
                Stop();
                mainWindow?.Dispatcher.Invoke(() =>
                {
                    mainWindow?.Logout();
                });
            }
        }

        // https://stackoverflow.com/a/5672897
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

        // ---

        private static TimeSpan? GetTimeUntilLock()
        {
            return SettingsManager.Settings.LockTime switch
            {
                LockTimeSetting.Never => null,
                LockTimeSetting.ThirtySeconds => TimeSpan.FromSeconds(30),
                LockTimeSetting.OneMinute => TimeSpan.FromMinutes(1),
                LockTimeSetting.FiveMinutes => TimeSpan.FromMinutes(5),
                LockTimeSetting.TenMinutes => TimeSpan.FromMinutes(10),
                LockTimeSetting.ThirtyMinutes => TimeSpan.FromMinutes(30),
                LockTimeSetting.OneHour => TimeSpan.FromHours(1),
                _ => null,
            };
        }
    }
}
