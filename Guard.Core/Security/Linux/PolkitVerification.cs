using System.Diagnostics;

namespace Guard.Core.Security.Linux
{
    /// <summary>
    /// Asks the desktop's polkit agent to verify the user (password, fingerprint or whatever PAM is
    /// configured with) before the keyring secret is used. This mirrors the Windows Hello prompt.
    /// Requires the app.2faguard.unlock action from linux/app.2faguard.policy to be installed.
    /// </summary>
    public static class PolkitVerification
    {
        public const string ActionId = "app.2faguard.unlock";
        private static readonly string[] policyDirectories =
        [
            "/usr/share/polkit-1/actions",
            "/usr/local/share/polkit-1/actions",
        ];

        public enum Result
        {
            Verified,
            Canceled,
            NotAvailable,
        }

        public static bool IsAvailable()
        {
            return FindPkcheck() != null
                && policyDirectories.Any(dir => File.Exists(Path.Combine(dir, $"{ActionId}.policy")));
        }

        public static async Task<Result> VerifyAsync()
        {
            string? pkcheck = FindPkcheck();
            if (pkcheck == null || !IsAvailable())
            {
                return Result.NotAvailable;
            }

            ProcessStartInfo startInfo = new(pkcheck)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("--action-id");
            startInfo.ArgumentList.Add(ActionId);
            startInfo.ArgumentList.Add("--process");
            startInfo.ArgumentList.Add(GetProcessSpec());
            startInfo.ArgumentList.Add("--allow-user-interaction");

            using Process process = Process.Start(startInfo)
                ?? throw new Exception("Failed to start pkcheck");
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return Result.Verified;
            }
            if (stderr.Contains("not registered", StringComparison.OrdinalIgnoreCase))
            {
                return Result.NotAvailable;
            }
            Log.Logger.Information(
                "polkit verification not granted (exit code {0}): {1}",
                process.ExitCode,
                stderr.Trim()
            );
            return Result.Canceled;
        }

        /// <summary>
        /// "pid,start-time,uid" so polkit can not be tricked by PID reuse.
        /// </summary>
        private static string GetProcessSpec()
        {
            int pid = Environment.ProcessId;
            string stat = File.ReadAllText($"/proc/{pid}/stat");
            // The command name can contain spaces, fields after the closing parenthesis are fixed
            string[] fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ');
            string startTime = fields[19];
            string uid = File.ReadAllLines($"/proc/{pid}/status")
                .First(l => l.StartsWith("Uid:", StringComparison.Ordinal))
                .Split('\t', StringSplitOptions.RemoveEmptyEntries)[1];
            return $"{pid},{startTime},{uid}";
        }

        private static string? FindPkcheck()
        {
            foreach (string dir in new[] { "/usr/bin", "/bin", "/usr/local/bin" })
            {
                string path = Path.Combine(dir, "pkcheck");
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }
    }
}
