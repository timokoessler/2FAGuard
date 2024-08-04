using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawHmacSecretSalt
    {
        public int FirstSize;
        public IntPtr First;
        public int SecondSize;
        public IntPtr Second;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawHmacSecretSaltOut : RawHmacSecretSalt, IDisposable
    {
        public RawHmacSecretSaltOut(byte[] first, byte[] second)
        {
            if (first == null || first.Length == 0)
                throw new ArgumentException("first salt cannot be empty");
            First = Marshal.AllocHGlobal(first.Length);
            Marshal.Copy(first, 0, First, first.Length);
            FirstSize = first.Length;
            if (second != null && second.Length != 0)
            {
                Second = Marshal.AllocHGlobal(second.Length);
                Marshal.Copy(second, 0, Second, second.Length);
                SecondSize = second.Length;
            }
        }

        protected void FreeMemory()
        {
            Helper.SafeFreeHGlobal(ref First);
            Helper.SafeFreeHGlobal(ref Second);
        }

        ~RawHmacSecretSaltOut() => FreeMemory();

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawCredWithHmacSecretSalt
    {
        public int CredIdSize;
        public IntPtr CredId;
        public IntPtr HmacSecretSaltRaw;

        public RawCredWithHmacSecretSalt(byte[] credentialId, RawHmacSecretSaltOut salts)
        {
            if (credentialId == null || credentialId.Length == 0)
                throw new ArgumentException("credentialId cannot be empty");
            CredId = Marshal.AllocHGlobal(credentialId.Length);
            Marshal.Copy(credentialId, 0, CredId, credentialId.Length);
            CredIdSize = credentialId.Length;

            HmacSecretSaltRaw = Marshal.AllocHGlobal(Marshal.SizeOf<RawHmacSecretSaltOut>());
            Marshal.StructureToPtr(salts, HmacSecretSaltRaw, false);
        }

        protected void FreeMemory()
        {
            Helper.SafeFreeHGlobal(ref CredId);
            Helper.SafeFreeHGlobal(ref HmacSecretSaltRaw);
        }

        ~RawCredWithHmacSecretSalt() => FreeMemory();

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawHmacSecretSaltValues
    {
        public IntPtr GlobalHmacSalt;

        public int SaltByCredCount;
        public IntPtr SaltByCredEntries;

        public RawHmacSecretSaltValues(
            RawHmacSecretSaltOut global,
            RawCredWithHmacSecretSalt[] byCred
        )
        {
            if (global != null)
            {
                GlobalHmacSalt = Marshal.AllocHGlobal(Marshal.SizeOf<RawHmacSecretSaltOut>());
                Marshal.StructureToPtr(global, GlobalHmacSalt, false);
            }
            if (byCred != null)
            {
                SaltByCredCount = byCred.Length;
                var entrySize = Marshal.SizeOf<RawCredWithHmacSecretSalt>();
                SaltByCredEntries = Marshal.AllocHGlobal(entrySize * SaltByCredCount);
                for (var n = 0; n < SaltByCredCount; n++)
                {
                    Marshal.StructureToPtr(byCred[n], SaltByCredEntries + n * entrySize, false);
                }
            }
        }

        protected void FreeMemory()
        {
            Helper.SafeFreeHGlobal(ref GlobalHmacSalt);
            Helper.SafeFreeHGlobal(ref SaltByCredEntries);
        }

        ~RawHmacSecretSaltValues() => FreeMemory();

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }
}
