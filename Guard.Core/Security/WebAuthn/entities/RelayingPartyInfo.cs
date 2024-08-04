using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class RelayingPartyInfo
    {
        // Version of this structure, to allow for modifications in the future.
        // This field is required and should be set to CURRENT_VERSION above.
        protected int StructVersion = 1;

        // Identifier for the RP. This field is required.
        public required string Id;

        // Contains the friendly name of the Relying Party, such as "Acme Corporation", "Widgets Inc" or "Awesome Site".
        // This field is required.
        public required string Name;

        // Optional URL pointing to RP's logo.
        public string? IconUrl;
    }
}
