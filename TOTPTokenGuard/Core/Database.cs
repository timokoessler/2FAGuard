using LiteDB;

namespace TOTPTokenGuard.Core
{
    class Database
    {
        private static LiteDatabase? db;

        private static string GetDBPath()
        {
            return System.IO.Path.Combine(Utils.GetAppDataFolderPath(), "TokenDatabase.db");
        }

        public static void Init()
        {
            db = new LiteDatabase($"Filename={GetDBPath()}");

            //var tokens = db.GetCollection<TOTPToken>("tokens");
        }

        public static bool FileExists()
        {
            return System.IO.File.Exists(GetDBPath());
        }
    }
}
