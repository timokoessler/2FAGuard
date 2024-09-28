#nullable disable
#pragma warning disable CS0649
using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawAuthenticatorMakeCredentialOptions
    {
        // Version of this structure, to allow for modifications in the future.
        protected int StructVersion = 5;

        // Time that the operation is expected to complete within.
        // This is used as guidance, and can be overridden by the platform.
        public int TimeoutMilliseconds;

        // Credentials used for exclusion.
        public RawCredentialsList ExcludeCredentialsList;

        // Optional extensions to parse when performing the operation.
        public RawWebAuthnExtensionsOut Extensions;

        // Optional. Platform vs Cross-Platform Authenticators.
        public AuthenticatorAttachment AuthenticatorAttachment;

        // Optional. Require key to be resident or not. Defaulting to FALSE;
        public bool RequireResidentKey;

        // User Verification Requirement.
        public UserVerificationRequirement UserVerificationRequirement;

        // Attestation Conveyance Preference.
        public AttestationConveyancePreference AttestationConveyancePreference;

        // Reserved for future Use
        protected int ReservedFlags = 0;

        // Cancellation Id - Optional - See WebAuthNGetCancellationId
        public IntPtr CancellationId;

        // @@ WEBAUTHN_AUTHENTICATOR_MAKE_CREDENTIAL_OPTIONS_VERSION_3 (API v1)

        // Exclude Credential List. If present, "CredentialList" will be ignored.
        public IntPtr ExcludeCredentialsExListPtr;

        // @@ WEBAUTHN_AUTHENTICATOR_MAKE_CREDENTIAL_OPTIONS_VERSION_4 (API v3)

        // Enterprise Attestation
        public EnterpriseAttestation EnterpriseAttestation;

        // Large Blob Support: none, required or preferred
        // InvalidParameter error when large blob required or preferred and
        // RequireResidentKey isn't set to TRUE
        public LargeBlobSupport LargeBlobSupport;

        // Optional. Prefer key to be resident. Defaulting to FALSE. When TRUE,
        // overrides the above RequireResidentKey.
        public bool PreferResidentKey;

        // @@ WEBAUTHN_AUTHENTICATOR_MAKE_CREDENTIAL_OPTIONS_VERSION_5 (API v4)

        // Optional. BrowserInPrivate Mode. Defaulting to FALSE.
        public bool BrowserInPrivateMode;

        // ------------ ignored ------------
        private readonly RawCredentialExList _excludeCredentialsExList;
        private readonly RawWebAuthnExtensionOut[] _rawExtensions;
        private readonly RawWebAuthnExtensionData[] _rawExtensionData;

        public RawAuthenticatorMakeCredentialOptions() { }

        public RawAuthenticatorMakeCredentialOptions(AuthenticatorMakeCredentialOptions makeOptions)
        {
            ExcludeCredentialsList = new RawCredentialsList(makeOptions.ExcludeCredentials);

            if (makeOptions.ExcludeCredentialsEx?.Count > 0)
            {
                _excludeCredentialsExList = new RawCredentialExList(
                    makeOptions.ExcludeCredentialsEx
                );
                ExcludeCredentialsExListPtr = Marshal.AllocHGlobal(
                    Marshal.SizeOf<RawCredentialExList>()
                );
                Marshal.StructureToPtr(
                    _excludeCredentialsExList,
                    ExcludeCredentialsExListPtr,
                    false
                );
            }

            CancellationId = IntPtr.Zero;
            if (makeOptions.CancellationId.HasValue)
            {
                CancellationId = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
                Marshal.StructureToPtr(makeOptions.CancellationId.Value, CancellationId, false);
            }

            TimeoutMilliseconds = makeOptions.TimeoutMilliseconds;
            AuthenticatorAttachment = makeOptions.AuthenticatorAttachment;
            UserVerificationRequirement = makeOptions.UserVerificationRequirement;
            AttestationConveyancePreference = makeOptions.AttestationConveyancePreference;
            RequireResidentKey = makeOptions.RequireResidentKey;

            var ex = makeOptions
                .Extensions?.Select(e => new { e.Type, Data = e.GetExtensionData() })
                .ToList();
            _rawExtensionData = ex?.Select(e => e.Data).ToArray();
            _rawExtensions = ex?.Select(e => new RawWebAuthnExtensionOut(e.Type, e.Data)).ToArray();
            Extensions = new RawWebAuthnExtensionsOut(_rawExtensions);

            EnterpriseAttestation = makeOptions.EnterpriseAttestation;
            LargeBlobSupport = makeOptions.LargeBlobSupport;
            PreferResidentKey = makeOptions.PreferResidentKey;
        }

        ~RawAuthenticatorMakeCredentialOptions() => FreeMemory();

        protected void FreeMemory()
        {
            ExcludeCredentialsList.Dispose();
            _excludeCredentialsExList?.Dispose();
            if (_rawExtensions != null)
                foreach (var ext in _rawExtensions)
                    ext.Dispose();
            if (_rawExtensionData != null)
                foreach (var ext in _rawExtensionData)
                    ext.Dispose();

            Helper.SafeFreeHGlobal(ref ExcludeCredentialsExListPtr);
            Helper.SafeFreeHGlobal(ref CancellationId);
        }

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }

    internal class AuthenticatorMakeCredentialOptions
    {
        // Time that the operation is expected to complete within.
        // This is used as guidance, and can be overridden by the platform.
        public int TimeoutMilliseconds = 30000;

        // Credentials used for exclusion.
        public ICollection<Credential> ExcludeCredentials;

        // Exclude Credential List. If present, "ExcludeCredentials" will be ignored.
        public ICollection<CredentialEx> ExcludeCredentialsEx;

        // Optional extensions to parse when performing the operation.
        public IReadOnlyCollection<WebAuthnCreationExtensionInput> Extensions;

        // Optional. Platform vs Cross-Platform Authenticators.
        public AuthenticatorAttachment AuthenticatorAttachment;

        // Optional. Require key to be resident or not. Defaulting to FALSE;
        public bool RequireResidentKey;

        // User Verification Requirement.
        public UserVerificationRequirement UserVerificationRequirement;

        // Attestation Conveyance Preference.
        public AttestationConveyancePreference AttestationConveyancePreference;

        // Cancellation Id - Optional - See WebAuthNGetCancellationId
        public Guid? CancellationId;

        // (API v3)

        // Enterprise Attestation
        public EnterpriseAttestation EnterpriseAttestation;

        // Large Blob Support: none, required or preferred
        // InvalidParamter error when large blob required or preferred and RequireResidentKey isn't set to TRUE
        public LargeBlobSupport LargeBlobSupport;

        // Optional. Prefer key to be resident. Defaulting to FALSE. When TRUE, overrides the above bRequireResidentKey.
        public bool PreferResidentKey;

        // (API v4)

        // Optional. BrowserInPrivate Mode. Defaulting to FALSE.
        public bool BrowserInPrivateMode;
    }
}
