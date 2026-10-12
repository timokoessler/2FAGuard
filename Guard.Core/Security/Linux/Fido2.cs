using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Guard.Core.Security.Linux
{
    public class Fido2Exception(int code, string message) : Exception(message)
    {
        public int Code { get; } = code;

        public bool IsCanceled => Code == Fido2.ErrKeepaliveCancel;
        public bool IsPinInvalid => Code == Fido2.ErrPinInvalid;
    }

    public class Fido2DeviceInfo
    {
        public required string Path { get; init; }
        public required string Name { get; init; }
        public bool HasPin { get; init; }
        public bool HasBuiltInUv { get; init; }
    }

    /// <summary>
    /// Minimal libfido2 binding for creating a credential with the hmac-secret extension and
    /// reading the hmac-secret output, which is what Windows' WebAuthn API is used for on Windows.
    /// </summary>
    public static unsafe class Fido2
    {
        private const string Lib = "libfido2.so.1";

        private const int FidoOk = 0;
        private const int FidoExtHmacSecret = 0x01;
        private const int FidoOptOmit = 0;
        private const int FidoOptFalse = 1;
        private const int FidoOptTrue = 2;
        private const int CoseEs256 = -7;
        private const int FidoDisableU2fFallback = 0x02;

        internal const int ErrCredentialExcluded = 0x19;
        internal const int ErrUnsupportedExtension = 0x16;
        internal const int ErrKeepaliveCancel = 0x2d;
        internal const int ErrNoCredentials = 0x2e;
        internal const int ErrActionTimeout = 0x2f;
        internal const int ErrPinInvalid = 0x31;
        internal const int ErrPinBlocked = 0x32;
        internal const int ErrPinAuthBlocked = 0x34;
        internal const int ErrPinNotSet = 0x35;
        internal const int ErrPinRequired = 0x36;

        private static readonly Lazy<bool> initialized = new(() =>
        {
            fido_init(FidoDisableU2fFallback);
            return true;
        });

        private static readonly Lock currentDeviceLock = new();
        private static IntPtr currentDevice;

        #region native
        [DllImport(Lib)]
        private static extern void fido_init(int flags);

        [DllImport(Lib)]
        private static extern IntPtr fido_strerr(int code);

        [DllImport(Lib)]
        private static extern IntPtr fido_dev_info_new(nuint n);

        [DllImport(Lib)]
        private static extern void fido_dev_info_free(ref IntPtr devlist, nuint n);

        [DllImport(Lib)]
        private static extern int fido_dev_info_manifest(IntPtr devlist, nuint ilen, out nuint olen);

        [DllImport(Lib)]
        private static extern IntPtr fido_dev_info_ptr(IntPtr devlist, nuint i);

        [DllImport(Lib)]
        private static extern IntPtr fido_dev_info_path(IntPtr di);

        [DllImport(Lib)]
        private static extern IntPtr fido_dev_info_manufacturer_string(IntPtr di);

        [DllImport(Lib)]
        private static extern IntPtr fido_dev_info_product_string(IntPtr di);

        [DllImport(Lib)]
        private static extern IntPtr fido_dev_new();

        [DllImport(Lib)]
        private static extern void fido_dev_free(ref IntPtr dev);

        [DllImport(Lib)]
        private static extern int fido_dev_open(IntPtr dev, string path);

        [DllImport(Lib)]
        private static extern int fido_dev_close(IntPtr dev);

        [DllImport(Lib)]
        private static extern int fido_dev_cancel(IntPtr dev);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool fido_dev_is_fido2(IntPtr dev);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool fido_dev_has_pin(IntPtr dev);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool fido_dev_has_uv(IntPtr dev);

        [DllImport(Lib)]
        private static extern int fido_dev_set_timeout(IntPtr dev, int ms);

        [DllImport(Lib)]
        private static extern IntPtr fido_cred_new();

        [DllImport(Lib)]
        private static extern void fido_cred_free(ref IntPtr cred);

        [DllImport(Lib)]
        private static extern int fido_cred_set_type(IntPtr cred, int cose);

        [DllImport(Lib)]
        private static extern int fido_cred_set_clientdata_hash(IntPtr cred, byte[] hash, nuint len);

        [DllImport(Lib)]
        private static extern int fido_cred_set_rp(IntPtr cred, string id, string name);

        [DllImport(Lib)]
        private static extern int fido_cred_set_user(
            IntPtr cred,
            byte[] userId,
            nuint userIdLen,
            string name,
            string displayName,
            IntPtr icon
        );

        [DllImport(Lib)]
        private static extern int fido_cred_set_extensions(IntPtr cred, int flags);

        [DllImport(Lib)]
        private static extern int fido_cred_set_rk(IntPtr cred, int opt);

        [DllImport(Lib)]
        private static extern int fido_cred_set_uv(IntPtr cred, int opt);

        [DllImport(Lib)]
        private static extern int fido_cred_exclude(IntPtr cred, byte[] id, nuint len);

        [DllImport(Lib)]
        private static extern int fido_dev_make_cred(IntPtr dev, IntPtr cred, string? pin);

        [DllImport(Lib)]
        private static extern IntPtr fido_cred_id_ptr(IntPtr cred);

        [DllImport(Lib)]
        private static extern nuint fido_cred_id_len(IntPtr cred);

        [DllImport(Lib)]
        private static extern IntPtr fido_assert_new();

        [DllImport(Lib)]
        private static extern void fido_assert_free(ref IntPtr assert);

        [DllImport(Lib)]
        private static extern int fido_assert_set_clientdata_hash(
            IntPtr assert,
            byte[] hash,
            nuint len
        );

        [DllImport(Lib)]
        private static extern int fido_assert_set_rp(IntPtr assert, string id);

        [DllImport(Lib)]
        private static extern int fido_assert_allow_cred(IntPtr assert, byte[] id, nuint len);

        [DllImport(Lib)]
        private static extern int fido_assert_set_extensions(IntPtr assert, int flags);

        [DllImport(Lib)]
        private static extern int fido_assert_set_hmac_salt(IntPtr assert, byte[] salt, nuint len);

        [DllImport(Lib)]
        private static extern int fido_assert_set_up(IntPtr assert, int opt);

        [DllImport(Lib)]
        private static extern int fido_assert_set_uv(IntPtr assert, int opt);

        [DllImport(Lib)]
        private static extern int fido_dev_get_assert(IntPtr dev, IntPtr assert, string? pin);

        [DllImport(Lib)]
        private static extern nuint fido_assert_count(IntPtr assert);

        [DllImport(Lib)]
        private static extern IntPtr fido_assert_id_ptr(IntPtr assert, nuint idx);

        [DllImport(Lib)]
        private static extern nuint fido_assert_id_len(IntPtr assert, nuint idx);

        [DllImport(Lib)]
        private static extern IntPtr fido_assert_hmac_secret_ptr(IntPtr assert, nuint idx);

        [DllImport(Lib)]
        private static extern nuint fido_assert_hmac_secret_len(IntPtr assert, nuint idx);
        #endregion

        public static bool IsSupported()
        {
            return NativeLibrary.TryLoad(Lib, out _);
        }

        public static List<Fido2DeviceInfo> ListDevices()
        {
            _ = initialized.Value;
            const int max = 16;
            List<Fido2DeviceInfo> result = [];
            IntPtr list = fido_dev_info_new(max);
            try
            {
                Check(fido_dev_info_manifest(list, max, out nuint count));
                for (nuint i = 0; i < count; i++)
                {
                    IntPtr di = fido_dev_info_ptr(list, i);
                    string path = Marshal.PtrToStringUTF8(fido_dev_info_path(di)) ?? "";
                    string name =
                        $"{Marshal.PtrToStringUTF8(fido_dev_info_manufacturer_string(di))} {Marshal.PtrToStringUTF8(fido_dev_info_product_string(di))}".Trim();

                    IntPtr dev = fido_dev_new();
                    try
                    {
                        if (fido_dev_open(dev, path) != FidoOk)
                        {
                            continue;
                        }
                        if (fido_dev_is_fido2(dev))
                        {
                            result.Add(
                                new Fido2DeviceInfo
                                {
                                    Path = path,
                                    Name = name,
                                    HasPin = fido_dev_has_pin(dev),
                                    HasBuiltInUv = fido_dev_has_uv(dev),
                                }
                            );
                        }
                        fido_dev_close(dev);
                    }
                    finally
                    {
                        fido_dev_free(ref dev);
                    }
                }
            }
            finally
            {
                fido_dev_info_free(ref list, max);
            }
            return result;
        }

        /// <summary>
        /// Cancels a running MakeCredential / GetAssertion operation that waits for user presence.
        /// </summary>
        public static void Cancel()
        {
            lock (currentDeviceLock)
            {
                if (currentDevice != IntPtr.Zero)
                {
                    fido_dev_cancel(currentDevice);
                }
            }
        }

        /// <summary>
        /// Windows uses the WebAuthn PRF extension, which hashes the salts before sending them to
        /// the authenticator as hmac-secret salts. Doing the same keeps keys usable on both systems.
        /// </summary>
        private static byte[] PrfSalt(byte[] input)
        {
            byte[] prefix = Encoding.UTF8.GetBytes("WebAuthn PRF");
            byte[] data = new byte[prefix.Length + 1 + input.Length];
            prefix.CopyTo(data, 0);
            input.CopyTo(data, prefix.Length + 1);
            return SHA256.HashData(data);
        }

        public static byte[] MakeCredential(
            Fido2DeviceInfo device,
            string? pin,
            string rpId,
            string rpName,
            byte[] userId,
            string userName,
            IEnumerable<byte[]> excludeCredentials
        )
        {
            _ = initialized.Value;
            EnsureUserVerification(device, pin);
            IntPtr cred = fido_cred_new();
            try
            {
                Check(fido_cred_set_type(cred, CoseEs256));
                byte[] cdh = RandomNumberGenerator.GetBytes(32);
                Check(fido_cred_set_clientdata_hash(cred, cdh, (nuint)cdh.Length));
                Check(fido_cred_set_rp(cred, rpId, rpName));
                Check(
                    fido_cred_set_user(
                        cred,
                        userId,
                        (nuint)userId.Length,
                        userName,
                        userName,
                        IntPtr.Zero
                    )
                );
                Check(fido_cred_set_extensions(cred, FidoExtHmacSecret));
                Check(fido_cred_set_rk(cred, FidoOptFalse));
                Check(fido_cred_set_uv(cred, pin == null ? FidoOptTrue : FidoOptOmit));
                foreach (byte[] id in excludeCredentials)
                {
                    Check(fido_cred_exclude(cred, id, (nuint)id.Length));
                }

                WithDevice(device, dev => Check(fido_dev_make_cred(dev, cred, pin)));

                int len = (int)fido_cred_id_len(cred);
                byte[] credentialId = new byte[len];
                Marshal.Copy(fido_cred_id_ptr(cred), credentialId, 0, len);
                return credentialId;
            }
            finally
            {
                fido_cred_free(ref cred);
            }
        }

        public static (byte[] credentialId, byte[] hmacSecret) GetHmacSecret(
            Fido2DeviceInfo device,
            string? pin,
            string rpId,
            IReadOnlyDictionary<byte[], (byte[] salt1, byte[] salt2)> saltsByCredential
        )
        {
            _ = initialized.Value;
            EnsureUserVerification(device, pin);

            // libfido2 only accepts one salt pair per assertion, so try the credentials one by one
            Fido2Exception? lastError = null;
            foreach (var (credentialId, salts) in saltsByCredential)
            {
                IntPtr assert = fido_assert_new();
                try
                {
                    byte[] cdh = RandomNumberGenerator.GetBytes(32);
                    Check(fido_assert_set_clientdata_hash(assert, cdh, (nuint)cdh.Length));
                    Check(fido_assert_set_rp(assert, rpId));
                    Check(fido_assert_allow_cred(assert, credentialId, (nuint)credentialId.Length));
                    Check(fido_assert_set_extensions(assert, FidoExtHmacSecret));
                    byte[] salt = [.. PrfSalt(salts.salt1), .. PrfSalt(salts.salt2)];
                    Check(fido_assert_set_hmac_salt(assert, salt, (nuint)salt.Length));
                    Check(fido_assert_set_up(assert, FidoOptTrue));
                    Check(fido_assert_set_uv(assert, pin == null ? FidoOptTrue : FidoOptOmit));

                    try
                    {
                        WithDevice(device, dev => Check(fido_dev_get_assert(dev, assert, pin)));
                    }
                    catch (Fido2Exception ex) when (ex.Code == ErrNoCredentials)
                    {
                        lastError = ex;
                        continue;
                    }

                    if (fido_assert_count(assert) < 1)
                    {
                        throw new Fido2Exception(-1, "The security key returned no assertion");
                    }
                    int idLen = (int)fido_assert_id_len(assert, 0);
                    byte[] returnedId = new byte[idLen];
                    if (idLen > 0)
                    {
                        Marshal.Copy(fido_assert_id_ptr(assert, 0), returnedId, 0, idLen);
                    }
                    else
                    {
                        returnedId = credentialId;
                    }

                    int secretLen = (int)fido_assert_hmac_secret_len(assert, 0);
                    if (secretLen != 64)
                    {
                        throw new Fido2Exception(
                            ErrUnsupportedExtension,
                            "HmacSecret is null. This normally means that your device does not support the HMAC secret extension."
                        );
                    }
                    byte[] secret = new byte[secretLen];
                    Marshal.Copy(fido_assert_hmac_secret_ptr(assert, 0), secret, 0, secretLen);
                    return (returnedId, secret);
                }
                finally
                {
                    fido_assert_free(ref assert);
                }
            }
            throw lastError
                ?? new Fido2Exception(ErrNoCredentials, GetErrorMessage(ErrNoCredentials));
        }

        private static void EnsureUserVerification(Fido2DeviceInfo device, string? pin)
        {
            // Windows requires user verification, which also changes the hmac-secret output
            if (pin == null && !device.HasBuiltInUv)
            {
                throw new Fido2Exception(
                    ErrPinNotSet,
                    device.HasPin
                        ? "A PIN is required for this security key."
                        : "Your security key has no PIN. Please set a PIN first (e.g. with the vendor tool or \"fido2-token -S\")."
                );
            }
        }

        private static void WithDevice(Fido2DeviceInfo device, Action<IntPtr> action)
        {
            IntPtr dev = fido_dev_new();
            try
            {
                Check(fido_dev_open(dev, device.Path));
                fido_dev_set_timeout(dev, 60_000);
                lock (currentDeviceLock)
                {
                    currentDevice = dev;
                }
                try
                {
                    action(dev);
                }
                finally
                {
                    lock (currentDeviceLock)
                    {
                        currentDevice = IntPtr.Zero;
                    }
                    fido_dev_close(dev);
                }
            }
            finally
            {
                fido_dev_free(ref dev);
            }
        }

        private static void Check(int code)
        {
            if (code != FidoOk)
            {
                throw new Fido2Exception(code, GetErrorMessage(code));
            }
        }

        private static string GetErrorMessage(int code)
        {
            return code switch
            {
                ErrPinInvalid => "Wrong PIN.",
                ErrPinBlocked or ErrPinAuthBlocked =>
                    "The PIN of your security key is blocked. Remove and reinsert the key or reset it.",
                ErrNoCredentials => "This security key is not registered in 2FAGuard.",
                ErrCredentialExcluded => "This security key is already registered.",
                ErrActionTimeout => "Timed out waiting for the security key to be touched.",
                ErrKeepaliveCancel => "Canceled",
                _ => Marshal.PtrToStringUTF8(fido_strerr(code)) ?? $"FIDO2 error {code}",
            };
        }
    }
}
