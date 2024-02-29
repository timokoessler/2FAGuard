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

    internal class AppSettings
    {
        public ThemeSetting Theme { get; set; } = ThemeSetting.System;
        public LanguageSetting Language { get; set; } = LanguageSetting.System;
    }
}
