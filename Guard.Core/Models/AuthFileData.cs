namespace Guard.Core.Models
{
    public class WebauthnDevice
    {
        public string? Id { get; set; }
        public string? EncryptedName { get; set; }
        public string? ProtectedKey { get; set; }
        public string? Salt1 { get; set; }
        public string? Salt2 { get; set; }
    }

    public class AuthFileData
    {
        public required int Version { get; set; }

        /// <summary>
        /// The installation ID of the current Guard installation is only used for supporting multiple Windows Hello accounts with the portable version.
        /// </summary>
        public required string InstallationID { get; set; }

        /// <summary>
        /// A challange to sign with Windows Hello to get a secret key.
        /// Inspired by Bitwarden's Windows Hello implementation.
        /// </summary>
        public string? WindowsHelloChallenge { get; set; }
        public string? PasswordProtectedKey { get; set; }
        public string? LoginSalt { get; set; }
        public string? KeySalt { get; set; }

        /// <summary>
        /// If the user selects to use the app without a password (insecure mode), this key is used to encrypt the data.
        /// This allows easier app design because the software has not to differentiate between a password-protected and an insecure installation.
        /// Additionally, this makes it slightly harder to access the sensitive data.
        /// </summary>
        public string? InsecureMainKey { get; set; }

        /// <summary>
        /// Stores a list of WebAuthn devices (external FIDO2 security keys, e.g. YubiKey).
        /// </summary>
        public List<WebauthnDevice>? WebAuthn { get; set; }
    }
}
