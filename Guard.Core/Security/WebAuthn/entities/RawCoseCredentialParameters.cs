using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class CoseCredentialParameter
    {
        // Version of this structure, to allow for modifications in the future.
        protected int StructVersion = 1;

        // Well-known credential type specifying a credential to create.
        protected IntPtr CredentialType = StringConstants.PublicKeyType;

        // Well-known COSE algorithm specifying the algorithm to use for the credential.
        public CoseAlgorithm Algorithm;

        public CoseCredentialParameter(CoseAlgorithm algo) => Algorithm = algo;

        public CoseCredentialParameter() { }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class RawCoseCredentialParameters : IDisposable
    {
        internal int Count;
        internal IntPtr Items;

        public RawCoseCredentialParameters() { }

        public RawCoseCredentialParameters(ICollection<CoseCredentialParameter> coseParams)
        {
            var cpSize = Marshal.SizeOf<CoseCredentialParameter>();
            Items = Marshal.AllocHGlobal(cpSize * coseParams.Count);
            Count = coseParams.Count;

            var pos = Items;
            foreach (var cp in coseParams)
            {
                Marshal.StructureToPtr(cp, pos, false);
                pos += cpSize;
            }
        }

        ~RawCoseCredentialParameters() => FreeMemory();

        protected void FreeMemory() => Helper.SafeFreeHGlobal(ref Items);

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }
}
