#nullable disable
using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredentialAttestation
    {
        protected int StructVersion;

        // Attestation format type
        public IntPtr FormatTypeStr;

        // Size of cbAuthenticatorData.
        public int AuthenticatorDataBytes;

        // Authenticator data that was created for this credential.
        public IntPtr AuthenticatorData;

        // Size of CBOR encoded attestation information
        //0 => encoded as CBOR null value.
        public int AttestationBytes;

        //Encoded CBOR attestation information
        public IntPtr Attestation;

        [MarshalAs(UnmanagedType.I4)]
        public AttestationDecodeType AttestationDecodeType;

        // Following depends on the dwAttestationDecodeType
        //  WEBAUTHN_ATTESTATION_DECODE_NONE
        //      NULL - not able to decode the CBOR attestation information
        //  WEBAUTHN_ATTESTATION_DECODE_COMMON
        //      PWEBAUTHN_COMMON_ATTESTATION;
        public IntPtr AttestationDecode;

        // The CBOR encoded Attestation Object to be returned to the RP.
        public int AttestationObjectBytes;
        public IntPtr AttestationObject;

        // The CredentialId bytes extracted from the Authenticator Data.
        // Used by Edge to return to the RP.
        public int CredentialIdBytes;
        public IntPtr CredentialId;

        public RawWebAuthnExtensionsIn Extensions;

        // @@ WEBAUTHN_CREDENTIAL_ATTESTATION_VERSION_3 (API v1)

        // One of the WEBAUTHN_CTAP_TRANSPORT_* bits will be set corresponding to
        // the transport that was used.
        [MarshalAs(UnmanagedType.I4)]
        public CtapTransport UsedTransport;

        // @@ WEBAUTHN_CREDENTIAL_ATTESTATION_VERSION_4 (API v3)

        public bool EnterpriseAttestation;
        public bool LargeBlobSupported;
        public bool ResidentKey;

        public CredentialAttestation MarshalToPublic()
        {
            var formatStr = Marshal.PtrToStringUni(FormatTypeStr);
            var type = EnumHelper.FromString<AttestationFormatType>(formatStr);

            var credId = new byte[CredentialIdBytes];
            Marshal.Copy(CredentialId, credId, 0, CredentialIdBytes);

            var authData = new byte[AuthenticatorDataBytes];
            Marshal.Copy(AuthenticatorData, authData, 0, AuthenticatorDataBytes);

            var attData = new byte[AttestationBytes];
            Marshal.Copy(Attestation, attData, 0, AttestationBytes);

            var atoData = new byte[AttestationObjectBytes];
            Marshal.Copy(AttestationObject, atoData, 0, AttestationObjectBytes);

            var rawCommonAtt =
                AttestationDecodeType == AttestationDecodeType.Common
                    ? Marshal.PtrToStructure<RawCommonAttestation>(AttestationDecode)
                    : null;

            var ext = Extensions.MarshalPublic(isCreation: true);

            return new CredentialAttestation
            {
                UsedTransport = UsedTransport,
                FormatType = type,
                Extensions = ext,
                CredentialId = credId,
                AuthenticatorData = authData,
                Attestation = attData,
                AttestationObject = atoData,
                CommonAttestation = rawCommonAtt?.MarshalToPublic(),
                EnterpriseAttestation = EnterpriseAttestation,
                LargeBlobSupported = LargeBlobSupported,
                ResidentKey = ResidentKey,
            };
        }
    }

    internal class CredentialAttestation
    {
        // Attestation format type
        public AttestationFormatType FormatType;

        // Authenticator data that was created for this credential.
        public byte[] AuthenticatorData;

        //Encoded CBOR attestation information
        public byte[] Attestation;

        public CommonAttestation CommonAttestation;

        // The CBOR encoded Attestation Object to be returned to the RP.
        public byte[] AttestationObject;

        // The CredentialId bytes extracted from the Authenticator Data.
        // Used by Edge to return to the RP.
        public byte[] CredentialId;

        public List<WebAuthnExtensionOutput> Extensions;

        // One of the WEBAUTHN_CTAP_TRANSPORT_* bits will be set corresponding to
        // the transport that was used.
        public CtapTransport UsedTransport;

        public bool EnterpriseAttestation;
        public bool LargeBlobSupported;
        public bool ResidentKey;
    }

    internal static class ExtensionListExtensions
    {
        public static T GetOrNull<T>(this List<WebAuthnExtensionOutput> list)
            where T : WebAuthnExtensionOutput
        {
            return list.FirstOrDefault(e => e as T != null) as T;
        }
    }
}
