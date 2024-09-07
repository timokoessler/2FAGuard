#nullable disable
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCommonAttestation
    {
        // Version of this structure, to allow for modifications in the future.
        protected int StructVersion = 1;

        // Hash and Padding Algorithm
        //
        // The following won't be set for "fido-u2f" which assumes "ES256".
        public IntPtr HashAlgorithm;

        [MarshalAs(UnmanagedType.I4)]
        public CoseAlgorithm CoseAlgorithm; // COSE algorithm

        // Signature that was generated for this attestation.
        public int SignatureBytes;
        public IntPtr Signature;

        // Following is set for Full Basic Attestation. If not, set then, this is Self Attestation.
        // Array of X.509 DER encoded certificates. The first certificate is the signer, leaf certificate.
        public int X5cCount;
        public IntPtr X5c; //WEBAUTHN_X5C[X5cCount]

        // Following are also set for tpm
        public IntPtr Version; // L"2.0"

        public int CertInfoBytes;
        public IntPtr CertInfo;

        public int PubAreaBytes;
        public IntPtr PubArea;

        public CommonAttestation MarshalToPublic()
        {
            var algoStr =
                (HashAlgorithm == IntPtr.Zero ? null : Marshal.PtrToStringUni(HashAlgorithm))
                ?? "ES256";
            if (CoseAlgorithm == 0)
                CoseAlgorithm = CoseAlgorithm.ECDSA_P256_WITH_SHA256;

            var signature = new byte[SignatureBytes];
            if (SignatureBytes > 0)
                Marshal.Copy(Signature, signature, 0, SignatureBytes);

            var certs = new List<X509Certificate2>();
            var pos = X5c;
            var x5cStep = Marshal.SizeOf<RawWebAuthnX5C>();
            for (var n = 0; n < X5cCount; n++)
            {
                var certBlock = Marshal.PtrToStructure<RawWebAuthnX5C>(pos);
                var data = new byte[certBlock.DataBytes];
                Marshal.Copy(certBlock.Data, data, 0, certBlock.DataBytes);
                var decoded = new X509Certificate2(data);
                certs.Add(decoded);
                pos += x5cStep;
            }

            var tpmVersion = Marshal.PtrToStringUni(Version);
            var certInfo = new byte[CertInfoBytes];
            if (CertInfoBytes > 0)
                Marshal.Copy(CertInfo, certInfo, 0, CertInfoBytes);
            var pubArea = new byte[PubAreaBytes];
            if (PubAreaBytes > 0)
                Marshal.Copy(PubArea, pubArea, 0, PubAreaBytes);
            return new CommonAttestation
            {
                Algorithm = algoStr,
                CoseAlgorithm = CoseAlgorithm,
                Signature = signature,
                Certificates = certs,
                TpmVersion = tpmVersion,
                TpmCertInfo = certInfo,
                TpmPubArea = pubArea
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawWebAuthnX5C
    {
        // Length of X.509 encoded certificate
        public int DataBytes;
        public IntPtr Data;
    }

    public class CommonAttestation
    {
        // Hash and Padding Algorithm
        public string Algorithm;
        public CoseAlgorithm CoseAlgorithm;

        // Signature that was generated for this attestation.
        public byte[] Signature;

        // Following is set for Full Basic Attestation. If not, set then, this is Self Attestation.
        // Array of X.509 DER encoded certificates. The first certificate is the signer, leaf certificate.
        public List<X509Certificate2> Certificates;

        // Following are also set for tpm
        public string TpmVersion;
        public byte[] TpmCertInfo;
        public byte[] TpmPubArea;
    }
}
