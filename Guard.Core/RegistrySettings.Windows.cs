using Microsoft.Win32;

namespace Guard.Core
{
    public partial class RegistrySettings
    {
        private static T? GetValue<T>(string keyPath, string value, T? defaultValue)
        {
            try
            {
                return (T?)Registry.GetValue(keyPath, value, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static bool GetValue(string keyPath, string value, bool defaultValue)
        {
            try
            {
                int? val = (int?)Registry.GetValue(keyPath, value, defaultValue ? 1 : 0);
                if (val == null)
                {
                    return defaultValue;
                }
                return val != 0;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
