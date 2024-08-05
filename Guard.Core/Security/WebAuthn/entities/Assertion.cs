using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawAssertion
    {
        // Version of this structure, to allow for modifications in the future.
        protected int StructVersion;

        // Size of cbAuthenticatorData.
        public int AuthenticatorDataBytes;

        // Authenticator data that was created for this assertion.
        public IntPtr AuthenticatorData;

        // Size of pbSignature.
        public int SignatureBytes;

        // Signature that was generated for this assertion.
        public IntPtr Signature;

        // Credential that was used for this assertion.
        public RawCredential Credential;

        // Size of User Id
        public int UserIdBytes;

        // UserId
        public IntPtr UserId;

        // @@ WEBAUTHN_ASSERTION_VERSION_2 (API v3)

        public RawWebAuthnExtensionsIn Extensions;

        // Size of pbCredLargeBlob
        public int CredLargeBlobSize;
        public IntPtr CredLargeBlob;

        public LargeBlobStatus CredLargeBlobStatus;

        // @@ WEBAUTHN_ASSERTION_VERSION_3 (API v4)

        IntPtr HmacSecret;

        public Assertion MarshalToPublic()
        {
            var authData = new byte[AuthenticatorDataBytes];
            if (AuthenticatorDataBytes > 0)
                Marshal.Copy(AuthenticatorData, authData, 0, AuthenticatorDataBytes);

            var sig = new byte[SignatureBytes];
            if (SignatureBytes > 0)
                Marshal.Copy(Signature, sig, 0, SignatureBytes);

            var uid = new byte[UserIdBytes];
            if (UserIdBytes > 0)
                Marshal.Copy(UserId, uid, 0, UserIdBytes);

            var cred = Credential.MarshalToPublic();

            List<WebAuthnExtensionOutput> ext;
            byte[] largeBlob;
            if (StructVersion >= 2)
            {
                ext = Extensions.MarshalPublic(isCreation: false);
                largeBlob = new byte[CredLargeBlobSize];
                if (CredLargeBlobSize > 0)
                    Marshal.Copy(CredLargeBlob, largeBlob, 0, CredLargeBlobSize);
            }
            else
            {
                ext = new List<WebAuthnExtensionOutput>();
                largeBlob = new byte[0];
            }

            HmacSecret hmacSecret = null;
            if (StructVersion >= 3)
            {
                if (HmacSecret != IntPtr.Zero)
                {
                    var rawSecret = Marshal.PtrToStructure<RawHmacSecretSalt>(HmacSecret);
                    hmacSecret = new HmacSecret()
                    {
                        First = new byte[rawSecret.FirstSize],
                        Second = new byte[rawSecret.SecondSize]
                    };
                    if (rawSecret.FirstSize > 0 && rawSecret.First != IntPtr.Zero)
                        Marshal.Copy(rawSecret.First, hmacSecret.First, 0, rawSecret.FirstSize);
                    if (rawSecret.SecondSize > 0 && rawSecret.Second != IntPtr.Zero)
                        Marshal.Copy(rawSecret.Second, hmacSecret.Second, 0, rawSecret.SecondSize);
                }
            }

            return new Assertion
            {
                AuthenticatorData = authData,
                Signature = sig,
                UserId = UserIdBytes == 0 ? null : uid,
                Credential = cred,
                Extensions = ext,
                LargeBlobStatus = CredLargeBlobStatus,
                LargeBlob = largeBlob,
                HmacSecret = hmacSecret
            };
        }
    }

    public class HmacSecret
    {
        public byte[] First,
            Second;
    }

    internal class Assertion
    {
        // Authenticator data that was created for this assertion.
        public byte[] AuthenticatorData;

        // Signature that was generated for this assertion.
        public byte[] Signature;

        // Credential that was used for this assertion.
        public Credential Credential;

        // UserId
        public byte[] UserId;

        // set to TRUE if the above U2fAppId from GetAssertionOptions was used instead of rpId
        public bool U2fAppIdUsed;

        public List<WebAuthnExtensionOutput> Extensions;

        public LargeBlobStatus LargeBlobStatus;
        public byte[] LargeBlob;
        public HmacSecret? HmacSecret;
    }
}
