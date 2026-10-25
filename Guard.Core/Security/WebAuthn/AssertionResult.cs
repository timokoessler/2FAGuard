namespace Guard.Core.Security.WebAuthn
{
    public class AssertionResult
    {
        public required byte[] CredentialId { get; set; }
        public required byte[] HmacSecret { get; set; }
    }
}
