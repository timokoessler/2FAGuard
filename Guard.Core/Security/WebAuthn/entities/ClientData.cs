#nullable disable
using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawClientData
    {
        // Version of this structure, to allow for modifications in the future.
        // This field is required and should be set to CURRENT_VERSION above.
        protected int StructVersion = 1;

        // Size of the pbClientDataJSON field.
        public int ClientDataJSONSize;

        // UTF-8 encoded JSON serialization of the client data.
        public IntPtr ClientDataJSON;

        // Hash algorithm ID used to hash the pbClientDataJSON field.
        public string HashAlgId;

        public RawClientData() { }

        public RawClientData(ClientData clientData)
        {
            ClientDataJSONSize = clientData.ClientDataJSON.Length;
            ClientDataJSON = Marshal.AllocHGlobal(ClientDataJSONSize);
            Marshal.Copy(
                clientData.ClientDataJSON,
                0,
                ClientDataJSON,
                clientData.ClientDataJSON.Length
            );
            HashAlgId = clientData.HashAlgorithm.GetString();
        }

        ~RawClientData() => FreeMemory();

        protected void FreeMemory() => Helper.SafeFreeHGlobal(ref ClientDataJSON);

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }

    internal class ClientData
    {
        // UTF-8 encoded JSON serialization of the client data.
        public required byte[] ClientDataJSON;

        // Hash algorithm ID used to hash the ClientDataJSON field.
        public HashAlgorithm HashAlgorithm;
    }
}
