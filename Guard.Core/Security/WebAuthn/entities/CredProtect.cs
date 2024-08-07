using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    internal enum UserVerification : int
    {
        Any = 0,
        Optional = 1,
        OptionalWithCredentialIdList = 2,
        Required = 3
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredProtectDataIn : RawWebAuthnExtensionData
    {
        public UserVerification UserVerification;
        public bool RequireCredProtect;
    }

    internal class CredProtectExtensionIn : WebAuthnCreationExtensionInput
    {
        public override ExtensionType Type => ExtensionType.CredProtect;

        public UserVerification UserVerification;

        // Rather fail than create an unprotected credential, if the extension is not supported
        public bool RequireCredProtect;

        public CredProtectExtensionIn(
            UserVerification userVerification = UserVerification.OptionalWithCredentialIdList,
            bool requireCredProtect = true
        )
        {
            UserVerification = userVerification;
            RequireCredProtect = requireCredProtect;
        }

        internal override RawWebAuthnExtensionData GetExtensionData() =>
            new RawCredProtectDataIn
            {
                UserVerification = UserVerification,
                RequireCredProtect = RequireCredProtect
            };
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredProtectDataOut : RawWebAuthnExtensionData
    {
        public UserVerification UserVerification;
    }

    internal class CredProtectExtensionOut : WebAuthnCreationExtensionOutput
    {
        public override ExtensionType Type => ExtensionType.CredProtect;
        public UserVerification UserVerification;

        static CredProtectExtensionOut() =>
            Register(
                ExtensionType.CredProtect,
                r =>
                {
                    var uv = UserVerification.Any;
                    if (r.ExtensionDataBytes > 0)
                        uv = Marshal
                            .PtrToStructure<RawCredProtectDataOut>(r.ExtensionData)
                            .UserVerification;
                    return new CredProtectExtensionOut { UserVerification = uv };
                }
            );
    }

    //// MakeCredential Input Type:   WEBAUTHN_CRED_PROTECT_EXTENSION_IN.
    ////      - pvExtension must point to a WEBAUTHN_CRED_PROTECT_EXTENSION_IN struct
    ////      - cbExtension will contain the sizeof(WEBAUTHN_CRED_PROTECT_EXTENSION_IN).
    //// MakeCredential Output Type:  DWORD.
    ////      - pvExtension will point to a DWORD with one of the above WEBAUTHN_USER_VERIFICATION_* values
    ////        if credential was successfully created with CRED_PROTECT.
    ////      - cbExtension will contain the sizeof(DWORD).
    //// GetAssertion Input Type:     Not Supported
    //// GetAssertion Output Type:    Not Supported
}
