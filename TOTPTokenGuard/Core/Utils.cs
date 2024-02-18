namespace TOTPTokenGuard.Core
{
    class Utils
    {
        public static string GetVersionString()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        public static string GetAppDataFolderPath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TOTPTokenGuard"
            );
        }
    }
}
