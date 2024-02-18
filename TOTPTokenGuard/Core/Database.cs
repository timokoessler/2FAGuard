using LiteDB;
using TOTPTokenGuard.Core.Models;
using TOTPTokenGuard.Core.Security;

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
            if (Auth.authData == null || Auth.dbEncryptionKey == null)
            {
                throw new System.Exception("Can not initialize database without AuthData");
            }

            db = new LiteDatabase($"Filename={GetDBPath()};Password={Auth.dbEncryptionKey}");

            //var tokens = db.GetCollection<TOTPToken>("tokens");
        }

        public static bool FileExists()
        {
            return System.IO.File.Exists(GetDBPath());
        }
    }
}
