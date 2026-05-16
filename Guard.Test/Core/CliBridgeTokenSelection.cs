using Guard.Core.CliBridge;
using Guard.Core.Models;

namespace Guard.Test.Core
{
    public class CliBridgeTokenSelection
    {
        [Fact]
        public void NumericValueSelectsTokenById()
        {
            DBTOTPToken token = CreateToken(4, "GitHub");

            CliBridgeTokenSelectionResult result = CliBridgeTokenSelector.Select(
                "4",
                id => id == 4 ? token : null,
                _ => []
            );

            Assert.True(result.Success);
            Assert.Same(token, result.Token);
        }

        [Fact]
        public void MissingNumericIdReturnsTokenNotFound()
        {
            CliBridgeTokenSelectionResult result = CliBridgeTokenSelector.Select(
                "4",
                _ => null,
                _ => []
            );

            Assert.False(result.Success);
            Assert.Equal(CliBridgeErrorCode.TokenNotFound, result.ErrorCode);
        }

        [Fact]
        public void IssuerSelectsSingleMatchingToken()
        {
            DBTOTPToken token = CreateToken(1, "GitHub");

            CliBridgeTokenSelectionResult result = CliBridgeTokenSelector.Select(
                "GitHub",
                _ => null,
                issuer => issuer == "GitHub" ? [token] : []
            );

            Assert.True(result.Success);
            Assert.Same(token, result.Token);
        }

        [Fact]
        public void UnknownIssuerReturnsTokenNotFound()
        {
            CliBridgeTokenSelectionResult result = CliBridgeTokenSelector.Select(
                "GitHub",
                _ => null,
                _ => []
            );

            Assert.False(result.Success);
            Assert.Equal(CliBridgeErrorCode.TokenNotFound, result.ErrorCode);
        }

        [Fact]
        public void DuplicateIssuerReturnsMultipleTokensFound()
        {
            CliBridgeTokenSelectionResult result = CliBridgeTokenSelector.Select(
                "GitHub",
                _ => null,
                _ => [CreateToken(1, "GitHub"), CreateToken(2, "GitHub")]
            );

            Assert.False(result.Success);
            Assert.Equal(CliBridgeErrorCode.MultipleTokensFound, result.ErrorCode);
        }

        private static DBTOTPToken CreateToken(int id, string issuer)
        {
            return new DBTOTPToken
            {
                Id = id,
                Issuer = issuer,
                EncryptedSecret = [],
                CreationTime = DateTime.UtcNow,
            };
        }
    }
}
