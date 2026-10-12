using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Guard.Linux.Core
{
    internal static partial class UnixSocket
    {
        internal readonly record struct PeerCredentials(int ProcessId, uint UserId);

        [StructLayout(LayoutKind.Sequential)]
        private struct UCred
        {
            public int pid;
            public uint uid;
            public uint gid;
        }

        private const int SOL_SOCKET = 1;
        private const int SO_PEERCRED = 17;

        [LibraryImport("libc", SetLastError = true)]
        private static partial int getsockopt(nint sockfd, int level, int optname, out UCred optval, ref uint optlen);

        [LibraryImport("libc")]
        private static partial uint geteuid();

        internal static uint GetEffectiveUserId() => geteuid();

        internal static PeerCredentials GetPeerCredentials(Socket socket)
        {
            uint len = (uint)Marshal.SizeOf<UCred>();
            if (getsockopt(socket.Handle, SOL_SOCKET, SO_PEERCRED, out UCred cred, ref len) != 0)
            {
                throw new IOException(
                    $"getsockopt(SO_PEERCRED) failed: {Marshal.GetLastPInvokeError()}"
                );
            }
            return new PeerCredentials(cred.pid, cred.uid);
        }

        /// <summary>
        /// Creates a listening socket that only the current user can connect to.
        /// </summary>
        internal static Socket Listen(string path)
        {
            ValidateSocketPath(path);
            FileInfo existing = new(path);
            if (existing.Exists)
            {
                if (existing.LinkTarget != null)
                {
                    throw new IOException($"Refusing to replace symbolic link at {path}");
                }
                existing.Delete();
            }
            Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                socket.Bind(new UnixDomainSocketEndPoint(path));
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                socket.Listen(16);
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Only sockets created by InstallationContext.GetPipeName may be replaced.
        /// </summary>
        private static void ValidateSocketPath(string path)
        {
            string fileName = Path.GetFileName(path);
            if (
                !Path.IsPathFullyQualified(path)
                || !string.Equals(Path.GetFullPath(path), path, StringComparison.Ordinal)
                || !fileName.StartsWith("2fa-guard-", StringComparison.Ordinal)
                || !fileName.EndsWith(".sock", StringComparison.Ordinal)
            )
            {
                throw new ArgumentException($"Invalid socket path: {path}", nameof(path));
            }
        }

        /// <summary>
        /// Returns the resolved path of the executable of a process.
        /// </summary>
        internal static string? GetProcessPath(int processId)
        {
            try
            {
                FileSystemInfo? target = File.ResolveLinkTarget($"/proc/{processId}/exe", false);
                return target?.FullName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
