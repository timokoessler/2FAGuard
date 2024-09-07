#nullable disable
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    public enum WebAuthnHResult : uint
    {
        Ok = 0,
        InvalidData = 0x8007000D, //Win32: The data is invalid. (Invalid salt length at hmac-secret extension)
        NotSupported = 0x80070032, //Win32: The request is not supported
        Canceled = 0x800704C7, //Win32: The operation was canceled by the user
        Timeout = 0x800705B4, //Win32: This operation returned because the timeout period expired
        NteExists = 0x8009000F, // Object already exists
        NteNotFound = 0x80090011, // Object was not found
        NteBadKeyset = 0x80090016, // Keyset does not exist (Windows Hello not active)
        NteTokenKeysetStorageFull = 0x80090023, // The security token does not have storage space available for an additional container
        NteInvalidParameter = 0x80090027, // The parameter is incorrect
        NteNotSupported = 0x80090029, // The requested operation is not supported
        NteDeviceNotFound = 0x80090035, // The device that is required by this cryptographic provider is not found on this platform
        NteUserCanceled = 0x80090036, // The action was cancelled by the user,
        SCardNoReadersAvailable = 0x8010002E // No NFC reader available?
    }

    public enum HashAlgorithm
    {
        [Description("SHA-256")]
        Sha256,

        [Description("SHA-384")]
        Sha384,

        [Description("SHA-512")]
        Sha512
    }

    public enum CredentialType
    {
        [Description("public-key")]
        PublicKey
    }

    public enum CoseAlgorithm : int
    {
        EDDSA = -8,
        ECDSA_P256_WITH_SHA256 = -7,
        ECDSA_P384_WITH_SHA384 = -35,
        ECDSA_P521_WITH_SHA512 = -36,

        RSASSA_PKCS1_V1_5_WITH_SHA256 = -257,
        RSASSA_PKCS1_V1_5_WITH_SHA384 = -258,
        RSASSA_PKCS1_V1_5_WITH_SHA512 = -259,

        RSA_PSS_WITH_SHA256 = -37,
        RSA_PSS_WITH_SHA384 = -38,
        RSA_PSS_WITH_SHA512 = -39
    }

    [Flags]
    public enum CtapTransport : int
    {
        NoRestrictions = 0,

        USB = 0x00000001,
        NFC = 0x00000002,
        BLE = 0x00000004,

        Test = 0x00000008,
        Internal = 0x00000010,
        Hybrid = 0x00000020,

        Mask = 0x0000001F
    }

    public enum AuthenticatorAttachment : int
    {
        Any = 0,
        Platform = 1,
        CrossPlatform = 2,
        CrossPlatformU2F = 3
    }

    public enum UserVerificationRequirement : int
    {
        Any = 0,
        Required = 1,
        Preferred = 2,
        Discouraged = 3
    }

    public enum AttestationConveyancePreference : int
    {
        Any = 0,
        None = 1,
        Indirect = 2,
        Direct = 3
    }

    public enum EnterpriseAttestation : int
    {
        None = 0,
        VendorFacilitated = 1,
        PlatformManaged = 2
    }

    public enum LargeBlobSupport : int
    {
        None = 0,
        Required = 1,
        Preferred = 2
    }

    public enum LargeBlobOperation : int
    {
        None = 0,
        Get = 1,
        Set = 2,
        Delete = 3
    }

    public enum LargeBlobStatus : int
    {
        None = 0,
        Success = 1,
        NotSupported = 2,
        InvalidData = 3,
        InvalidParameter = 4,
        NotFound = 5,
        MultipleCredentials = 6,
        LackOfSpace = 7,
        PlatformError = 8,
        AuthenticationError = 9
    }

    public enum GetAssertionFlags : int
    {
        HmacSecretValues = 0x00100000
    }

    public enum AttestationFormatType
    {
        [Description("packed")]
        Packed,

        [Description("fido-u2f")]
        U2F,

        [Description("tpm")]
        TPM,

        [Description("none")]
        None
    }

    public enum ExtensionType
    {
        [Description("hmac-secret")]
        HmacSecret,

        [Description("credProtect")]
        CredProtect,

        [Description("credBlob")]
        CredBlob,

        [Description("minPinLength")]
        MinPinLength
    }

    public enum AttestationDecodeType
    {
        None = 0,
        Common = 1

        // WEBAUTHN_ATTESTATION_DECODE_COMMON supports format types
        //  L"packed"
        //  L"fido-u2f"
    }

    internal static class EnumHelper
    {
        private static (
            Dictionary<int, string> Forward,
            Dictionary<string, int> Reverse
        ) BuildDict<T>()
        {
            var enumType = typeof(T);
            var values = (T[])Enum.GetValues(enumType);
            var forward = values
                .Select(v =>
                    (
                        Value: v,
                        Attr: enumType
                            .GetField(v.ToString())
                            .GetCustomAttribute<DescriptionAttribute>()
                    )
                )
                .Where(t => t.Attr != null)
                .ToDictionary(t => Convert.ToInt32(t.Value), t => t.Attr.Description);
            return (forward, forward.ToDictionary(kv => kv.Value, kv => kv.Key));
        }

        private static readonly ConcurrentDictionary<
            Type,
            (Dictionary<int, string> Forward, Dictionary<string, int> Reverse)
        > Cache =
            new ConcurrentDictionary<Type, (Dictionary<int, string>, Dictionary<string, int>)>();

        public static string GetString<T>(this T value)
        {
            var (forward, _) = Cache.GetOrAdd(typeof(T), t => BuildDict<T>());
            if (!forward.TryGetValue(Convert.ToInt32(value), out var desc))
                throw new ArgumentException(
                    $"No Description found for value {value} on type {typeof(T).Name}"
                );
            return desc;
        }

        public static T FromString<T>(string str)
        {
            var (_, reverse) = Cache.GetOrAdd(typeof(T), t => BuildDict<T>());
            if (!reverse.TryGetValue(str, out var value))
                throw new ArgumentException(
                    $"No Value found for string {str} on type {typeof(T).Name}"
                );
            return (T)(object)value;
        }
    }

    internal static class StringConstants
    {
        public static IntPtr PublicKeyType;

        static StringConstants()
        {
            PublicKeyType = Marshal.StringToHGlobalUni(CredentialType.PublicKey.GetString());
        }
    }

    internal static class Helper
    {
        public static void SafeFreeHGlobal(ref IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return;
            Marshal.FreeHGlobal(ptr);
            ptr = IntPtr.Zero;
        }
    }
}
