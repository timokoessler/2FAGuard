using System.Runtime.InteropServices;

namespace Guard.Core.Security.Linux
{
    /// <summary>
    /// Stores secrets in the freedesktop Secret Service (GNOME Keyring, KWallet, KeePassXC, ...)
    /// through libsecret. Used as the Linux counterpart of the Windows Credential Manager.
    /// </summary>
    public static unsafe class SecretServiceKeyring
    {
        private const string LibSecret = "libsecret-1.so.0";
        private const string LibGLib = "libglib-2.0.so.0";
        private const string SchemaName = "app.2faguard.QuickUnlock";
        private const string ApplicationAttribute = "application";
        private const string InstallationAttribute = "installation";

        // SecretSchema: name, flags, SecretSchemaAttribute[32] { name, type }, reserved int, 7 reserved pointers
        private const int SchemaSize = 8 + 8 + (32 * 16) + 8 + (7 * 8);
        private static IntPtr schema;
        private static readonly Lock schemaLock = new();

        [DllImport(LibSecret)]
        private static extern int secret_password_storev_sync(
            IntPtr schema,
            IntPtr attributes,
            IntPtr collection,
            IntPtr label,
            IntPtr password,
            IntPtr cancellable,
            out IntPtr error
        );

        [DllImport(LibSecret)]
        private static extern IntPtr secret_password_lookupv_sync(
            IntPtr schema,
            IntPtr attributes,
            IntPtr cancellable,
            out IntPtr error
        );

        [DllImport(LibSecret)]
        private static extern int secret_password_clearv_sync(
            IntPtr schema,
            IntPtr attributes,
            IntPtr cancellable,
            out IntPtr error
        );

        [DllImport(LibSecret)]
        private static extern void secret_password_free(IntPtr password);

        [DllImport(LibGLib)]
        private static extern IntPtr g_hash_table_new(IntPtr hashFunc, IntPtr keyEqualFunc);

        [DllImport(LibGLib)]
        private static extern int g_hash_table_insert(IntPtr table, IntPtr key, IntPtr value);

        [DllImport(LibGLib)]
        private static extern void g_hash_table_unref(IntPtr table);

        [DllImport(LibGLib)]
        private static extern void g_error_free(IntPtr error);

        public static bool IsLibraryAvailable()
        {
            return NativeLibrary.TryLoad(LibSecret, out _) && NativeLibrary.TryLoad(LibGLib, out _);
        }

        /// <summary>
        /// Checks whether a Secret Service provider is reachable on the session bus.
        /// </summary>
        public static bool IsAvailable()
        {
            if (!IsLibraryAvailable())
            {
                return false;
            }
            try
            {
                _ = Lookup("availability-check");
                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Secret Service is not available: {0}", ex.Message);
                return false;
            }
        }

        public static void Store(string installationId, string label, string secret)
        {
            WithAttributes(
                installationId,
                attributes =>
                {
                    IntPtr labelPtr = Marshal.StringToCoTaskMemUTF8(label);
                    IntPtr secretPtr = Marshal.StringToCoTaskMemUTF8(secret);
                    try
                    {
                        int ok = secret_password_storev_sync(
                            GetSchema(),
                            attributes,
                            IntPtr.Zero,
                            labelPtr,
                            secretPtr,
                            IntPtr.Zero,
                            out IntPtr error
                        );
                        ThrowIfError(error);
                        if (ok == 0)
                        {
                            throw new Exception("Failed to store the secret in the keyring");
                        }
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(labelPtr);
                        ZeroAndFree(secretPtr, System.Text.Encoding.UTF8.GetByteCount(secret));
                    }
                    return 0;
                }
            );
        }

        public static string? Lookup(string installationId)
        {
            return WithAttributes(
                installationId,
                attributes =>
                {
                    IntPtr result = secret_password_lookupv_sync(
                        GetSchema(),
                        attributes,
                        IntPtr.Zero,
                        out IntPtr error
                    );
                    ThrowIfError(error);
                    if (result == IntPtr.Zero)
                    {
                        return null;
                    }
                    try
                    {
                        return Marshal.PtrToStringUTF8(result);
                    }
                    finally
                    {
                        secret_password_free(result);
                    }
                }
            );
        }

        public static void Clear(string installationId)
        {
            WithAttributes(
                installationId,
                attributes =>
                {
                    _ = secret_password_clearv_sync(
                        GetSchema(),
                        attributes,
                        IntPtr.Zero,
                        out IntPtr error
                    );
                    ThrowIfError(error);
                    return 0;
                }
            );
        }

        private static T WithAttributes<T>(string installationId, Func<IntPtr, T> action)
        {
            IntPtr glib = NativeLibrary.Load(LibGLib);
            IntPtr table = g_hash_table_new(
                NativeLibrary.GetExport(glib, "g_str_hash"),
                NativeLibrary.GetExport(glib, "g_str_equal")
            );
            IntPtr[] strings =
            [
                Marshal.StringToCoTaskMemUTF8(ApplicationAttribute),
                Marshal.StringToCoTaskMemUTF8("2FAGuard"),
                Marshal.StringToCoTaskMemUTF8(InstallationAttribute),
                Marshal.StringToCoTaskMemUTF8(installationId),
            ];
            try
            {
                g_hash_table_insert(table, strings[0], strings[1]);
                g_hash_table_insert(table, strings[2], strings[3]);
                return action(table);
            }
            finally
            {
                g_hash_table_unref(table);
                foreach (IntPtr s in strings)
                {
                    Marshal.FreeCoTaskMem(s);
                }
            }
        }

        private static IntPtr GetSchema()
        {
            lock (schemaLock)
            {
                if (schema != IntPtr.Zero)
                {
                    return schema;
                }
                // Lives for the whole process lifetime because libsecret keeps references to it
                IntPtr mem = Marshal.AllocHGlobal(SchemaSize);
                new Span<byte>((void*)mem, SchemaSize).Clear();
                byte* p = (byte*)mem;
                *(IntPtr*)p = Marshal.StringToHGlobalAnsi(SchemaName);
                // flags = SECRET_SCHEMA_NONE, attribute type = SECRET_SCHEMA_ATTRIBUTE_STRING (both 0)
                *(IntPtr*)(p + 16) = Marshal.StringToHGlobalAnsi(ApplicationAttribute);
                *(IntPtr*)(p + 32) = Marshal.StringToHGlobalAnsi(InstallationAttribute);
                schema = mem;
                return schema;
            }
        }

        private static void ThrowIfError(IntPtr error)
        {
            if (error == IntPtr.Zero)
            {
                return;
            }
            string message =
                Marshal.PtrToStringUTF8(*(IntPtr*)((byte*)error + 8)) ?? "Unknown keyring error";
            g_error_free(error);
            throw new Exception(message);
        }

        private static void ZeroAndFree(IntPtr ptr, int byteCount)
        {
            new Span<byte>((void*)ptr, byteCount).Clear();
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}
