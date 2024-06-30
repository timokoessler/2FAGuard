using Guard.Core.Security;

namespace Guard.Core.Storage
{
    public class Reset
    {
        public static void DeleteEverything()
        {
            string path = InstallationContext.GetAppDataFolderPath();
            Database.Deinit();
            _ = WindowsHello.Unregister();
            Auth.DeleteWindowsHelloProtectedKey();

            string[] files = ["auth-keys", "settings", "TokenDatabase.db", "TokenDatabase-log.db"];
            string[] dirs = ["icons"];

            foreach (string file in files)
            {
                string filePath = Path.Combine(path, file);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            foreach (string dir in dirs)
            {
                string dirPath = Path.Combine(path, dir);
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }
            }
        }
    }
}
