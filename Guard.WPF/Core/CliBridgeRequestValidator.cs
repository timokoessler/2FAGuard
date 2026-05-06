using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using Guard.Core.CliBridge;

namespace Guard.WPF.Core
{
    internal static class CliBridgeRequestValidator
    {
        private const string CodeSigningThumbprint = "43E9DD4D4A06853CBB521EA35E5F337EDB0DBCCD";

        internal static string? GetError(CliBridgeRequest request)
        {
            string? requestError = CliBridgeProtocolValidator.GetRequestError(request);
            if (requestError != null)
            {
                return requestError;
            }

            string? processPath = GetProcessPath(request.RequestingProcessId);
            if (
                processPath == null
                || request.RequestingProcessPath == null
                || !Path.GetFullPath(processPath)
                    .Equals(Path.GetFullPath(request.RequestingProcessPath), StringComparison.OrdinalIgnoreCase)
            )
            {
                return CliBridgeErrorCode.Unauthorized;
            }

            string fileName = Path.GetFileName(processPath);
            if (
                !fileName.Equals("2fa.exe", StringComparison.OrdinalIgnoreCase)
                && !fileName.Equals("2fa-Preview.exe", StringComparison.OrdinalIgnoreCase)
            )
            {
                return CliBridgeErrorCode.Unauthorized;
            }

            string expectedCliPath = Path.Combine(AppContext.BaseDirectory, fileName);
            if (
                !File.Exists(expectedCliPath)
                || !Path.GetFullPath(expectedCliPath)
                    .Equals(Path.GetFullPath(processPath), StringComparison.OrdinalIgnoreCase)
            )
            {
                return CliBridgeErrorCode.Unauthorized;
            }

            if (!IsTrustedBinary(processPath))
            {
                return CliBridgeErrorCode.Unauthorized;
            }

            return null;
        }

        private static string? GetProcessPath(int processId)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                return process.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsTrustedBinary(string path)
        {
#if DEBUG
            return true;
#else
            return WinVerifyTrust(path) && HasExpectedSigner(path);
#endif
        }

        private static bool HasExpectedSigner(string path)
        {
            try
            {
#pragma warning disable SYSLIB0057
                using X509Certificate signedFileCertificate = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
                using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
                    signedFileCertificate.Export(X509ContentType.Cert)
                );
                return certificate
                    .Thumbprint.Equals(CodeSigningThumbprint, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool WinVerifyTrust(string filePath)
        {
            Guid action = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
            WINTRUST_FILE_INFO fileInfo = new()
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                pcwszFilePath = filePath,
            };
            WINTRUST_DATA data = new()
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                dwUIChoice = 2,
                fdwRevocationChecks = 0,
                dwUnionChoice = 1,
                pFile = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>()),
                dwStateAction = 0,
                dwProvFlags = 0x10,
            };

            try
            {
                Marshal.StructureToPtr(fileInfo, data.pFile, false);
                return WinVerifyTrust(IntPtr.Zero, ref action, ref data) == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(data.pFile);
            }
        }

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] ref Guid pgActionID,
            ref WINTRUST_DATA pWVTData
        );

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }
    }
}
