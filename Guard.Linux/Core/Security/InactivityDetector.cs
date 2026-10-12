using Avalonia.Threading;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Storage;

namespace Guard.Linux.Core.Security
{
    /// <summary>
    /// Locks the app after the configured time without user input while the app is focused,
    /// or after the configured time since the app lost the focus.
    /// </summary>
    internal class InactivityDetector
    {
        private static DateTime? lostFocusTime;
        private static DateTime lastInputTime = DateTime.UtcNow;
        private static DispatcherTimer? timer;
        private static LockTimeSetting? forcedLockTime;
        private static Action? onLock;

        public static void Init(Action lockAction)
        {
            onLock = lockAction;
        }

        public static void OnFocusLost()
        {
            lostFocusTime ??= DateTime.UtcNow;
        }

        public static void OnFocusGained()
        {
            lostFocusTime = null;
            lastInputTime = DateTime.UtcNow;
        }

        public static void OnUserInput()
        {
            lastInputTime = DateTime.UtcNow;
        }

        public static void Start()
        {
            forcedLockTime = RegistrySettings.GetForcedLockTime();
            if ((forcedLockTime ?? SettingsManager.Settings.LockTime) == LockTimeSetting.Never)
            {
                return;
            }
            Stop();
            lastInputTime = DateTime.UtcNow;
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += TimerTick;
            timer.Start();
        }

        public static void Stop()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= TimerTick;
                timer = null;
            }
        }

        public static bool IsRunning()
        {
            return timer?.IsEnabled ?? false;
        }

        private static void TimerTick(object? sender, EventArgs e)
        {
            TimeSpan? timeUntilLock = GetTimeUntilLock();
            if (timeUntilLock == null)
            {
                return;
            }
            DateTime now = DateTime.UtcNow;
            bool inactive = lostFocusTime == null && now - lastInputTime > timeUntilLock.Value;
            bool unfocusedTooLong =
                lostFocusTime != null && lostFocusTime.Value.Add(timeUntilLock.Value) < now;
            if (inactive || unfocusedTooLong)
            {
                Stop();
                Log.Logger.Information("Locking app because of inactivity");
                onLock?.Invoke();
            }
        }

        private static TimeSpan? GetTimeUntilLock()
        {
            LockTimeSetting activeLockTimeSetting =
                forcedLockTime ?? SettingsManager.Settings.LockTime;
            return activeLockTimeSetting switch
            {
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
