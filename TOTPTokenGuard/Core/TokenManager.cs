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
                //Todo  Load data from database and create TOTPTokenHelper objects
                tokenHelpers = [];
                DBTOTPToken test =
                    new()
                    {
                        Id = 1,
                        Issuer = "ACME",
                        EncryptedSecret = Security
                            .Auth.GetMainEncryptionHelper()
                            .EncryptString("ABCDEFGH")
                    };
                tokenHelpers.Add(new TOTPTokenHelper(test));
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

        public static void AddToken(TOTPTokenHelper token)
        {
            tokenHelpers ??= [];
            tokenHelpers.Add(token);

            // Todo: Save to database
        }

        public static void ClearTokens()
        {
            tokenHelpers = null;
        }
    }
}
