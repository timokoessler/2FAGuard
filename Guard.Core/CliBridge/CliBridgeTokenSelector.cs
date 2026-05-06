using Guard.Core.Models;

namespace Guard.Core.CliBridge
{
    public class CliBridgeTokenSelectionResult
    {
        public bool Success { get; set; }
        public DBTOTPToken? Token { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public static CliBridgeTokenSelectionResult Ok(DBTOTPToken token)
        {
            return new CliBridgeTokenSelectionResult { Success = true, Token = token };
        }

        public static CliBridgeTokenSelectionResult Error(string errorCode, string errorMessage)
        {
            return new CliBridgeTokenSelectionResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
            };
        }
    }

    public static class CliBridgeTokenSelector
    {
        public static CliBridgeTokenSelectionResult Select(
            string issuerOrId,
            Func<int, DBTOTPToken?> getTokenById,
            Func<string, List<DBTOTPToken>> getTokensByIssuer
        )
        {
            if (int.TryParse(issuerOrId, out int id))
            {
                DBTOTPToken? token = getTokenById(id);
                if (token == null)
                {
                    return CliBridgeTokenSelectionResult.Error(
                        CliBridgeErrorCode.TokenNotFound,
                        "No token found with the specified ID."
                    );
                }
                return CliBridgeTokenSelectionResult.Ok(token);
            }

            List<DBTOTPToken> tokens = getTokensByIssuer(issuerOrId);
            if (tokens.Count > 1)
            {
                return CliBridgeTokenSelectionResult.Error(
                    CliBridgeErrorCode.MultipleTokensFound,
                    "Multiple tokens found with the specified issuer. Use the token ID instead."
                );
            }
            if (tokens.Count == 0)
            {
                return CliBridgeTokenSelectionResult.Error(
                    CliBridgeErrorCode.TokenNotFound,
                    "No token found with the specified issuer."
                );
            }

            return CliBridgeTokenSelectionResult.Ok(tokens[0]);
        }
    }
}
