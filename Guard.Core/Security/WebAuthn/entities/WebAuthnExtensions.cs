#nullable disable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal abstract class RawWebAuthnExtensionData : IDisposable
    {
        public virtual void Dispose() { }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawWebAuthnExtension
    {
        public string ExtensionIdentifier;
        public int ExtensionDataBytes;
        public IntPtr ExtensionData;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawWebAuthnExtensionOut : RawWebAuthnExtension
    {
        public RawWebAuthnExtensionOut(ExtensionType type, RawWebAuthnExtensionData data)
        {
            ExtensionIdentifier = EnumHelper.GetString(type);
            ExtensionDataBytes = Marshal.SizeOf(data);
            if (ExtensionDataBytes == 0)
                return;
            ExtensionData = Marshal.AllocHGlobal(ExtensionDataBytes);
            Marshal.StructureToPtr(data, ExtensionData, false);
        }

        ~RawWebAuthnExtensionOut() => FreeMemory();

        protected void FreeMemory()
        {
            Helper.SafeFreeHGlobal(ref ExtensionData);
        }

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }

#nullable enable

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawWebAuthnExtensionIn : RawWebAuthnExtension
    {
        public WebAuthnExtensionOutput? MarshalPublic(bool isCreation)
        {
            var type = EnumHelper.FromString<ExtensionType>(ExtensionIdentifier);
            return WebAuthnExtensionOutput.Get(type, isCreation, this);
        }
    }

#nullable disable

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawWebAuthnExtensionsOut
    {
        public int Count = 0;
        public IntPtr Extensions;

        public RawWebAuthnExtensionsOut(IReadOnlyCollection<RawWebAuthnExtensionOut> extensions)
        {
            Count = extensions?.Count ?? 0;
            if (Count == 0)
                return;
            var elmSize = Marshal.SizeOf<RawWebAuthnExtension>();
            var pos = Extensions = Marshal.AllocHGlobal(Count * elmSize);
            foreach (var ext in extensions)
            {
                Marshal.StructureToPtr(ext, pos, false);
                pos += elmSize;
            }
        }

        ~RawWebAuthnExtensionsOut() => FreeMemory();

        protected void FreeMemory()
        {
            if (Extensions == IntPtr.Zero)
                return;
            Helper.SafeFreeHGlobal(ref Extensions);
        }

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawWebAuthnExtensionsIn
    {
        public int Count = 0;
        public IntPtr Extensions = IntPtr.Zero;

        public List<WebAuthnExtensionOutput> MarshalPublic(bool isCreation)
        {
            var extensions = new List<WebAuthnExtensionOutput>();
            if (Count == 0 || Extensions == IntPtr.Zero)
                return extensions;

            var pos = Extensions;
            var elmSize = Marshal.SizeOf<RawWebAuthnExtension>();
            for (var n = 0; n < Count; n++)
            {
                var ext = Marshal.PtrToStructure<RawWebAuthnExtensionIn>(pos);
                var parsedExt = ext.MarshalPublic(isCreation);
                if (parsedExt != null)
                {
                    extensions.Add(parsedExt);
                }
                pos += elmSize;
            }
            return extensions;
        }
    }

    internal abstract class WebAuthnExtension
    {
        public abstract ExtensionType Type { get; }

        static WebAuthnExtension()
        {
            //initialize static ctors of extension types, to fill the dictionary in WebAuthnExtensionOutput
            var extTypes = typeof(WebAuthnExtension).Assembly.ExportedTypes.Where(t =>
                typeof(WebAuthnExtension).IsAssignableFrom(t)
            );
            foreach (var extType in extTypes)
                RuntimeHelpers.RunClassConstructor(extType.TypeHandle);
        }
    }

#nullable enable

    internal abstract class WebAuthnExtensionOutput : WebAuthnExtension
    {
        internal static WebAuthnExtensionOutput? Get(
            ExtensionType type,
            bool isCreation,
            RawWebAuthnExtensionIn raw
        )
        {
            if (type == ExtensionType.HmacSecret)
            {
                if (isCreation)
                {
                    return HmacSecretResultExtension.Parse(raw);
                }
            }
            if (type == ExtensionType.CredProtect)
            {
                if (isCreation)
                {
                    return CredProtectExtensionOut.Parse(raw);
                }
            }

            Log.Logger.Error(
                "Unsupported extension type {0} in {1}",
                type,
                isCreation ? "creation" : "assertion"
            );

            return null;
        }
    }

#nullable disable

    internal abstract class WebAuthnExtensionInput : WebAuthnExtension
    {
        internal abstract RawWebAuthnExtensionData GetExtensionData();
    }

    internal abstract class WebAuthnCreationExtensionInput : WebAuthnExtensionInput { }

    internal abstract class WebAuthnAssertionExtensionInput : WebAuthnExtensionInput { }

    internal abstract class WebAuthnCreationExtensionOutput : WebAuthnExtensionOutput { }

    internal abstract class WebAuthnAssertionExtensionOutput : WebAuthnExtensionOutput { }
}
