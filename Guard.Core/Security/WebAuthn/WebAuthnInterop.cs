using System.Reflection;
using System.Runtime.InteropServices;
using Guard.Core.Security.WebAuthn.entities;

/**
 * The webauthn implementation is based on the Win32 webauthn.dll
 * https://github.com/microsoft/webauthn
 * Strongly inspired by the following project:
 * https://github.com/dbeinder/Yoq.WindowsWebAuthn
 */
namespace Guard.Core.Security.WebAuthn
{
    internal class WebAuthnInterop
    {
        private static int? _apiVersion;

        public static int GetApiVersion()
        {
            if (_apiVersion == null)
            {
                if (!CheckApiAvailable())
                {
                    throw new PlatformNotSupportedException(
                        "WebAuthn API is not available on this platform."
                    );
                }
                _apiVersion = WebAuthNGetApiVersionNumber();
            }
            return _apiVersion
                ?? throw new PlatformNotSupportedException("Can not get WebAuthn API version.");
        }

        public static bool CheckApiAvailable()
        {
            if (_apiVersion != null)
            {
                return true;
            }
            var getApiVersionMethod = typeof(WebAuthnInterop).GetMethod(
                nameof(WebAuthNGetApiVersionNumber),
                BindingFlags.Public | BindingFlags.Static
            );
            if (getApiVersionMethod == null)
            {
                return false;
            }
            try
            {
                Marshal.Prelink(getApiVersionMethod);
            }
            catch
            {
                return false;
            }
            return true;
        }

        [DllImport("webauthn.dll", CharSet = CharSet.Unicode)]
        public static extern int WebAuthNGetApiVersionNumber();

        [DllImport("webauthn.dll", CharSet = CharSet.Unicode)]
        private static extern int WebAuthNAuthenticatorMakeCredential(
            [In] IntPtr hWnd,
            [In] RelayingPartyInfo rpInfo,
            [In] RawUserInfo rawUserInfo,
            [In] RawCoseCredentialParameters rawCoseCredParams,
            [In] RawClientData rawClientData,
            [In, Optional] RawAuthenticatorMakeCredentialOptions rawMakeCredentialOptions,
            [Out] out IntPtr rawCredentialAttestation
        );

        [DllImport("webauthn.dll")]
        private static extern void WebAuthNFreeCredentialAttestation(
            IntPtr rawCredentialAttestation
        );

        public static WebAuthnHResult AuthenticatorMakeCredential(
            IntPtr window,
            RelayingPartyInfo rp,
            UserInfo user,
            ICollection<CoseCredentialParameter> coseParams,
            ClientData clientData,
            AuthenticatorMakeCredentialOptions makeOptions,
            out CredentialAttestation? credential
        )
        {
            credential = null;

            var rawUser = new RawUserInfoOut(user);
            var rawCredList = new RawCoseCredentialParameters(coseParams);
            var rawClientData = new RawClientData(clientData);
            var rawMakeCredOptions =
                makeOptions == null ? null : new RawAuthenticatorMakeCredentialOptions(makeOptions);

            var res = WebAuthNAuthenticatorMakeCredential(
                window,
                rp,
                rawUser,
                rawCredList,
                rawClientData,
                rawMakeCredOptions,
                out var rawCredPtr
            );

            if (rawCredPtr != IntPtr.Zero)
            {
                var rawCredObj = Marshal.PtrToStructure<RawCredentialAttestation>(rawCredPtr);
                credential = rawCredObj?.MarshalToPublic();
                WebAuthNFreeCredentialAttestation(rawCredPtr);
            }

            rawUser.Dispose();
            rawCredList.Dispose();
            rawClientData.Dispose();
            rawMakeCredOptions?.Dispose();

            return (WebAuthnHResult)res;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, IntPtr ZeroOnly);
    }
}
