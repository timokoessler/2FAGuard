using Guard.Core.Models;
using LiteDB;

namespace Guard.Core.Storage
{
    public class Database
    {
        private static LiteDatabase? db;
        private static ILiteCollection<DBTOTPToken>? tokens;

        private static string GetDBPath()
        {
            return Path.Combine(InstallationContext.GetAppDataFolderPath(), "TokenDatabase.db");
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

        public static void Deinit()
        {
            if (db == null)
            {
                return;
            }
            db.Dispose();
            db = null;
            tokens = null;
        }

        public static bool FileExists()
        {
            return File.Exists(GetDBPath());
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

        public static List<DBTOTPToken> GetTokensByIssuer(string issuer)
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            return tokens.Find(t => t.Issuer == issuer).ToList();
        }

        public static DBTOTPToken GetTokenById(int id)
        {
            if (tokens == null)
            {
                throw new Exception("Database not initialized");
            }
            return tokens.FindById(id);
        }
    }
}
