using LiteDB;
using Guard.Core.Models;

namespace Guard.Core.Storage
{
    class Database
    {
        private static LiteDatabase? db;
        private static ILiteCollection<DBTOTPToken>? tokens;

        private static string GetDBPath()
        {
            return System.IO.Path.Combine(Utils.GetAppDataFolderPath(), "TokenDatabase.db");
        }

        public static void Init()
        {
            if (db != null)
            {
                return;
            }
            db = new LiteDatabase($"Filename={GetDBPath()}") { UserVersion = 1 };

            tokens = db.GetCollection<DBTOTPToken>("tokens");
        }

        public static bool FileExists()
        {
            return System.IO.File.Exists(GetDBPath());
        }

        public static List<DBTOTPToken> GetAllTokens()
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            return tokens.FindAll().ToList();
        }

        public static void AddToken(DBTOTPToken token)
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            tokens.Insert(token);
        }

        public static void DeleteTokenById(int id)
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            tokens.Delete(id);
        }
    }
}
