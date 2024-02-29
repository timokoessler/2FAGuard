using System.Diagnostics;
using System.Reflection;
using System.Web;

namespace Guard.Core.Aptabase;

internal class SystemInfo
{
    public static bool IsInDebugMode()
    {
#if DEBUG
        return true;
#endif
#pragma warning disable CS0162 // Unerreichbarer Code wurde entdeckt.
        return false;
#pragma warning restore CS0162 // Unerreichbarer Code wurde entdeckt.
    }

    public static string GetOsName()
    {
        return "Windows";
    }

    public static string GetOsVersion()
    {
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            return $"Windows 11 {Environment.OSVersion.Version.Build}";
        }
        return $"Windows 10 {Environment.OSVersion.Version.Build}";
    }

    public static string GetLocale()
    {
        return System.Globalization.CultureInfo.CurrentCulture.Name;
    }
}
