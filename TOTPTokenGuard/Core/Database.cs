using LiteDB;

namespace TOTPTokenGuard.Core
{
    class Database
    {
        private static LiteDatabase? db;

        private static string GetDBPath()
        {
            return System.IO.Path.Combine(
                System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TOTPTokenGuard",
                "TOTPTokenGuard.db"
            );
        }

        public static void InitDB(String encryptionKey)
        {
            db = new LiteDatabase($"Filename={GetDBPath()};Password={encryptionKey}");
        }

        public static bool FileExists()
        {
            return System.IO.File.Exists(GetDBPath());
        }
    }
}
