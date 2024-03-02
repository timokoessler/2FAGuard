using Guard.Core.Models;
using Guard.Core.Storage;

namespace Guard.Core
{
    class TokenManager
    {
        private static List<TOTPTokenHelper>? tokenHelpers;

        internal static async Task LoadDataInBackground()
        {
            await Task.Run(() =>
            {
                List<DBTOTPToken> dbTokens = Database.GetAllTokens();
                tokenHelpers = [];
                foreach (DBTOTPToken dbToken in dbTokens)
                {
                    tokenHelpers.Add(new TOTPTokenHelper(dbToken));
                }
            });
        }

        internal static async Task<List<TOTPTokenHelper>?> GetAllTokens()
        {
            if (tokenHelpers == null)
            {
                await LoadDataInBackground();
            }
            return tokenHelpers;
        }

        internal static int GetNextId()
        {
            if (tokenHelpers == null)
            {
                throw new Exception("TokenHelpers not loaded");
            }
            if (tokenHelpers.Count == 0)
            {
                return 1;
            }
            return tokenHelpers.Max(token => token.dBToken.Id) + 1;
        }

        internal static bool AddToken(DBTOTPToken dbToken)
        {
            if (tokenHelpers == null)
            {
                throw new Exception("TokenHelpers not loaded");
            }
            var tokenHelper = new TOTPTokenHelper(dbToken);
            if (IsDuplicate(tokenHelper))
            {
                return false;
            }
            tokenHelpers.Add(tokenHelper);
            Database.AddToken(dbToken);
            return true;
        }

        internal static void ClearTokens()
        {
            tokenHelpers = null;
        }

        internal static TOTPTokenHelper? GetTokenById(int id)
        {
            if (tokenHelpers == null)
            {
                throw new Exception("TokenHelpers not loaded");
            }
            return tokenHelpers.Find(token => token.dBToken.Id == id);
        }

        internal static void DeleteTokenById(int id)
        {
            if (tokenHelpers == null)
            {
                throw new Exception("TokenHelpers not loaded");
            }
            TOTPTokenHelper? token = tokenHelpers.Find(token => token.dBToken.Id == id);
            if (token != null)
            {
                tokenHelpers.Remove(token);
                Database.DeleteTokenById(id);
            }
        }

        internal static bool IsDuplicate(TOTPTokenHelper token)
        {
            if (tokenHelpers == null)
            {
                throw new Exception("TokenHelpers not loaded");
            }
            return tokenHelpers.Any(t => token.decryptedSecret == t.decryptedSecret);
        }
    }
}
