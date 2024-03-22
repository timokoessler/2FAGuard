using Guard.Core.Installation;
using Guard.Core.Models;
using LiteDB;

namespace Guard.Core.Storage
{
    class Database
    {
        private static LiteDatabase? db;
        private static ILiteCollection<DBTOTPToken>? tokens;

        private static string GetDBPath()
        {
            return System.IO.Path.Combine(
                InstallationInfo.GetAppDataFolderPath(),
                "TokenDatabase.db"
            );
        }

        internal static void Init()
        {
            if (db != null)
            {
                return;
            }
            db = new LiteDatabase($"Filename={GetDBPath()}") { UserVersion = 1 };

            tokens = db.GetCollection<DBTOTPToken>("tokens");
        }

        internal static void Deinit()
        {
            if (db == null)
            {
                return;
            }
            db.Dispose();
            db = null;
            tokens = null;
        }

        internal static bool FileExists()
        {
            return System.IO.File.Exists(GetDBPath());
        }

        internal static List<DBTOTPToken> GetAllTokens()
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            return tokens.FindAll().ToList();
        }

        internal static void AddToken(DBTOTPToken token)
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            tokens.Insert(token);
        }

        internal static void DeleteTokenById(int id)
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            tokens.Delete(id);
        }
    }
}
