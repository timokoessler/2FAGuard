using TOTPTokenGuard.Core.Models;

namespace TOTPTokenGuard.Core
{
    class TokenManager
    {
        private static List<TOTPTokenHelper>? tokenHelpers;

        public static async Task LoadDataInBackground()
        {
            await Task.Run(() =>
            {
                List<DBTOTPToken> dbTokens = Database.GetAllTokens();
                tokenHelpers = new List<TOTPTokenHelper>();
                foreach (DBTOTPToken dbToken in dbTokens)
                {
                    tokenHelpers.Add(new TOTPTokenHelper(dbToken));
                }
            });
        }

        public static async Task<List<TOTPTokenHelper>?> GetAllTokens()
        {
            if (tokenHelpers == null)
            {
                await LoadDataInBackground();
            }
            return tokenHelpers;
        }

        public static int GetNextId()
        {
            if (tokenHelpers == null)
            {
                throw new Exception("TokenHelpers not loaded");
            }
            return tokenHelpers.Count + 1;
        }

        public static void AddToken(DBTOTPToken dbToken)
        {
            if (tokenHelpers == null)
            {
                throw new Exception("TokenHelpers not loaded");
            }
            tokenHelpers.Add(new TOTPTokenHelper(dbToken));
            Database.AddToken(dbToken);
        }

        public static void ClearTokens()
        {
            tokenHelpers = null;
        }
    }
}
