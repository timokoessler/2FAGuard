namespace Guard.Core.Models
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
        DE
    }

    internal enum SortOrderSetting
    {
        ISSUER_ASC,
        ISSUER_DESC,
        CREATED_ASC,
        CREATED_DESC
    }

    internal class AppSettings
    {
        public ThemeSetting Theme { get; set; } = ThemeSetting.System;
        public LanguageSetting Language { get; set; } = LanguageSetting.System;
        public bool PreventRecording { get; set; } = true;
        public bool LockOnScreenLock { get; set; } = true;
        public SortOrderSetting SortOrder { get; set; } = SortOrderSetting.ISSUER_ASC;
        public bool ShowTokenCardIntro { get; set; } = true;
    }
}
