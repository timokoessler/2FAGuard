using Guard.Core.CliBridge;

namespace Guard.Test.Core
{
    public class CliBridgeProtocol
    {
        [Fact]
        public void ValidGetCodeRequestIsAccepted()
        {
            CliBridgeRequest request = new()
            {
                Version = CliBridgeProtocolConstants.Version,
                Operation = CliBridgeOperation.GetCode,
                IssuerOrId = "GitHub",
            };

            Assert.Null(CliBridgeProtocolValidator.GetRequestError(request));
        }

        [Fact]
        public void UnknownVersionIsRejected()
        {
            CliBridgeRequest request = new()
            {
                Version = 2,
                Operation = CliBridgeOperation.GetCode,
                IssuerOrId = "GitHub",
            };

            Assert.Equal(
                CliBridgeErrorCode.UnsupportedVersion,
                CliBridgeProtocolValidator.GetRequestError(request)
            );
        }

        [Fact]
        public void UnknownOperationIsRejected()
        {
            CliBridgeRequest request = new()
            {
                Version = CliBridgeProtocolConstants.Version,
                Operation = "DeleteToken",
                IssuerOrId = "GitHub",
            };

            Assert.Equal(
                CliBridgeErrorCode.UnsupportedOperation,
                CliBridgeProtocolValidator.GetRequestError(request)
            );
        }

        [Fact]
        public void EmptyIssuerOrIdIsRejected()
        {
            CliBridgeRequest request = new()
            {
                Version = CliBridgeProtocolConstants.Version,
                Operation = CliBridgeOperation.GetCode,
                IssuerOrId = " ",
            };

            Assert.Equal(
                CliBridgeErrorCode.InvalidRequest,
                CliBridgeProtocolValidator.GetRequestError(request)
            );
        }

        [Fact]
        public void ErrorResponseDoesNotSerializeCode()
        {
            CliBridgeResponse response = CliBridgeResponse.Error(
                CliBridgeErrorCode.Locked,
                "Desktop app is locked."
            );
            string json = CliBridgeSerializer.SerializeResponse(response);

            Assert.DoesNotContain("\"Code\"", json);
            Assert.DoesNotContain("123456", json);
        }

        [Fact]
        public void InvalidJsonRequestReturnsNull()
        {
            Assert.Null(CliBridgeSerializer.DeserializeRequest("{not json"));
        }

        [Fact]
        public void InvalidJsonResponseReturnsNull()
        {
            Assert.Null(CliBridgeSerializer.DeserializeResponse("{not json"));
        }
    }
}
