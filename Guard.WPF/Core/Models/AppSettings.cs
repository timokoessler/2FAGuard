using Guard.WPF.Core.Installation;

namespace Guard.WPF.Core.Models
{
    internal enum ThemeSetting
    {
        System,
        Light,
        Dark
    }

    internal enum LanguageSetting
    {
        System,
        EN,
        DE,
        ZH_CN,
        ZH_TW
    }

    internal enum SortOrderSetting
    {
        ISSUER_ASC,
        ISSUER_DESC,
        CREATED_ASC,
        CREATED_DESC
    }

    internal enum LockTimeSetting
    {
        Never,
        ThirtySeconds,
        OneMinute,
        FiveMinutes,
        TenMinutes,
        ThirtyMinutes,
        OneHour
    }

    internal class AppSettings
    {
        public ThemeSetting Theme { get; set; } = ThemeSetting.System;
        public LanguageSetting Language { get; set; } = LanguageSetting.System;
        public bool PreventRecording { get; set; } = true;
        public bool LockOnScreenLock { get; set; } = true;
        public SortOrderSetting SortOrder { get; set; } = SortOrderSetting.ISSUER_ASC;
        public bool ShowTokenCardIntro { get; set; } = true;
        public bool MinimizeToTray { get; set; } = false;
        public LockTimeSetting LockTime { get; set; } = LockTimeSetting.TenMinutes;
        public Version LastUsedAppVersion { get; set; } = new(0, 0);
    }
}
