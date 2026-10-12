#if !WINDOWS
using System.Diagnostics;

namespace Guard.CLI.Core
{
    /// <summary>
    /// A terminal process can not own the clipboard after it exits, so the text is handed to a
    /// clipboard tool that keeps serving it in the background.
    /// </summary>
    internal static class LinuxClipboard
    {
        internal static void SetText(string text)
        {
            bool wayland = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
            (string tool, string[] args)[] candidates = wayland
                ?
                [
                    ("wl-copy", []),
                    ("xclip", ["-selection", "clipboard"]),
                    ("xsel", ["--clipboard", "--input"]),
                ]
                :
                [
                    ("xclip", ["-selection", "clipboard"]),
                    ("xsel", ["--clipboard", "--input"]),
                    ("wl-copy", []),
                ];

            foreach (var (tool, args) in candidates)
            {
                string? path = FindInPath(tool);
                if (path == null)
                {
                    continue;
                }
                // The background process must not inherit redirected pipes or the terminal,
                // otherwise waiting for the foreground part blocks until it exits
                ProcessStartInfo startInfo = new("/bin/sh")
                {
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                };
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add("exec \"$0\" \"$@\" >/dev/null 2>&1");
                startInfo.ArgumentList.Add(path);
                foreach (string arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }
                using Process process =
                    Process.Start(startInfo) ?? throw new Exception($"Failed to start {tool}");
                process.StandardInput.Write(text);
                process.StandardInput.Close();
                // The tools fork into the background, so only wait for the foreground part
                if (!process.WaitForExit(3000) || process.ExitCode == 0)
                {
                    return;
                }
            }

            throw new Exception(
                "No clipboard tool found. Please install wl-clipboard (Wayland) or xclip (X11)."
            );
        }

        private static string? FindInPath(string name)
        {
            string[] paths = (Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin").Split(
                ':'
            );
            return paths.Select(p => Path.Combine(p, name)).FirstOrDefault(File.Exists);
        }
    }
}
#endif
