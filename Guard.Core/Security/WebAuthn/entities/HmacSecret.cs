using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class HmacSecretBoolData : RawWebAuthnExtensionData
    {
        public bool Bool;
    }

    internal class HmacSecretCreationExtension : WebAuthnCreationExtensionInput
    {
        public override ExtensionType Type => ExtensionType.HmacSecret;

        internal override RawWebAuthnExtensionData GetExtensionData() =>
            new HmacSecretBoolData { Bool = true };
    }

    internal class HmacSecretResultExtension : WebAuthnCreationExtensionOutput
    {
        public override ExtensionType Type => ExtensionType.HmacSecret;
        public bool Success;

        static HmacSecretResultExtension() =>
            Register(
                ExtensionType.HmacSecret,
                r =>
                {
                    var success = false;
                    if (r.ExtensionDataBytes > 0)
                        success = Marshal.PtrToStructure<HmacSecretBoolData>(r.ExtensionData).Bool;
                    return new HmacSecretResultExtension { Success = success };
                }
            );
    }

    public class PrfSalt
    {
        public byte[] First,
            Second;
    }

    internal class HmacSecretAssertionExtension : WebAuthnAssertionExtensionInput
    {
        public bool UseRawSalts = false;
        public PrfSalt GlobalSalt;
        public Dictionary<byte[], PrfSalt> SaltsByCredential;
        public override ExtensionType Type => ExtensionType.HmacSecret;

        internal override RawWebAuthnExtensionData GetExtensionData() =>
            new HmacSecretBoolData { Bool = true };
    }

    internal class HmacSecretAssertionResultExtension : WebAuthnAssertionExtensionOutput
    {
        public override ExtensionType Type => ExtensionType.HmacSecret;
        public byte[] First,
            Second;
    }
}
