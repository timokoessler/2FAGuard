using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredential
    {
        // Version of this structure, to allow for modifications in the future.
        protected int StructVersion = 1;

        // Size of pbID.
        public int CredentialIdBytes;

        // Unique ID for this particular credential.
        public IntPtr CredentialId;

        // Well-known credential type specifying what this particular credential is.
        public IntPtr CredentialType = StringConstants.PublicKeyType;

        public Credential MarshalToPublic()
        {
            var credType = Marshal.PtrToStringUni(CredentialType);
            var typeEnum = EnumHelper.FromString<CredentialType>(credType);

            var cid = new byte[CredentialIdBytes];
            if (CredentialIdBytes > 0)
                Marshal.Copy(CredentialId, cid, 0, CredentialIdBytes);
            return new Credential { CredentialId = cid, CredentialType = typeEnum };
        }
    }

    internal class Credential
    {
        // Unique ID for this particular credential.
        public byte[] CredentialId;

        // Well-known credential type specifying what this particular credential is.
        public CredentialType CredentialType = CredentialType.PublicKey;

        public Credential() { }

        public Credential(byte[] credentialId) => CredentialId = credentialId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredentialsList
    {
        public int Count;
        public IntPtr Credentials;

        public RawCredentialsList() { }

        public RawCredentialsList(ICollection<Credential> credList)
        {
            Count = credList?.Count ?? 0;
            if (Count == 0)
                return;

            var idDataSize = credList.Sum(c => c.CredentialId.Length);
            var elmSize = Marshal.SizeOf<RawCredential>();
            Credentials = Marshal.AllocHGlobal(elmSize * Count + idDataSize);
            var pos = Credentials;
            var idPos = pos + Count * elmSize;
            foreach (var cred in credList)
            {
                Marshal.Copy(cred.CredentialId, 0, idPos, cred.CredentialId.Length);
                var rawCred = new RawCredential
                {
                    CredentialId = idPos,
                    CredentialIdBytes = cred.CredentialId.Length
                };
                idPos += cred.CredentialId.Length;
                Marshal.StructureToPtr(rawCred, pos, false);
                pos += elmSize;
            }
        }

        ~RawCredentialsList() => FreeMemory();

        protected void FreeMemory() => Helper.SafeFreeHGlobal(ref Credentials);

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }
}
