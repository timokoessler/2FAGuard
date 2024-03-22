using Guard.Core.Installation;
using System.IO;

namespace Guard.Core.Storage
{
    internal class Reset
    {
        internal static void DeleteEverything()
        {
            string path = InstallationInfo.GetAppDataFolderPath();
            Database.Deinit();

            string[] files = ["auth-keys", "settings", "TokenDatabase.db", "TokenDatabase-log.db"];

            foreach (string file in files)
            {
                string filePath = Path.Combine(path, file);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}
