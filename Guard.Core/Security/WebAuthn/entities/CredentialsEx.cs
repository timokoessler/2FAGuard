#nullable disable
using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredentialEx
    {
        // Version of this structure, to allow for modifications in the future.
        protected int StructVersion = 1;

        // Size of pbID.
        public int CredentialIdBytes;

        // Unique ID for this particular credential.
        public IntPtr CredentialId;

        // Well-known credential type specifying what this particular credential is.
        public IntPtr CredentialType = StringConstants.PublicKeyType;

        // Transports. 0 implies no transport restrictions.
        public CtapTransport Transports;
    }

    internal class CredentialEx
    {
        // Unique ID for this particular credential.
        public byte[] CredentialId;

        // Well-known credential type specifying what this particular credential is.
        public CredentialType CredentialType;

        // Transports. 0 implies no transport restrictions.
        public CtapTransport AllowedTransports;

        public CredentialEx() { }

        public CredentialEx(byte[] credId, CtapTransport allowedAllowedTransports)
        {
            CredentialId = credId;
            AllowedTransports = allowedAllowedTransports;
            CredentialType = CredentialType.PublicKey;
        }

        public CredentialEx(
            byte[] credId,
            CredentialType type,
            CtapTransport allowedAllowedTransports
        )
            : this(credId, allowedAllowedTransports) => CredentialType = type;
    }

    // Information about credential list with extra information
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredentialExList
    {
        public int Count;
        public IntPtr RawCredentialsEx; //list of pointers(!) to the actual credentials

        public RawCredentialExList() { }

        public RawCredentialExList(ICollection<CredentialEx> credList)
        {
            Count = credList?.Count ?? 0;
            if (Count == 0)
                return;

            var idDataSize = credList.Sum(c => c.CredentialId.Length);
            var elmSize = Marshal.SizeOf<RawCredentialEx>();
            RawCredentialsEx = Marshal.AllocHGlobal((elmSize + IntPtr.Size) * Count + idDataSize);

            var ptrListPos = RawCredentialsEx;
            var elmPos = ptrListPos + IntPtr.Size * Count;
            var idPos = elmPos + elmSize * Count;

            foreach (var cred in credList)
            {
                Marshal.Copy(cred.CredentialId, 0, idPos, cred.CredentialId.Length);

                var rawCred = new RawCredentialEx
                {
                    Transports = cred.AllowedTransports,
                    CredentialId = idPos,
                    CredentialIdBytes = cred.CredentialId.Length
                };

                Marshal.StructureToPtr(rawCred, elmPos, false);
                Marshal.WriteIntPtr(ptrListPos, elmPos);

                ptrListPos += IntPtr.Size;
                elmPos += elmSize;
                idPos += cred.CredentialId.Length;
            }
        }

        ~RawCredentialExList() => FreeMemory();

        protected void FreeMemory() => Helper.SafeFreeHGlobal(ref RawCredentialsEx);

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }
}
