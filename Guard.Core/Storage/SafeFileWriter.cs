using System.Collections.Concurrent;

namespace Guard.Core.Storage
{
    public class SafeFileWriter
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

        /// <summary>
        /// Saves the byte array content to the target file atomically using File.Replace asynchronously.
        /// Uses a temp file named {originalFileName}.{random}.tmp and backup with .bak extension.
        /// </summary>
        /// <param name="targetFilePath">The path of the file to save.</param>
        /// <param name="content">The byte array content to write.</param>
        public static async Task SaveFileAsync(string targetFilePath, byte[] content)
        {
            targetFilePath = ValidateTargetFilePath(targetFilePath);
            var fileLock = _fileLocks.GetOrAdd(targetFilePath, _ => new SemaphoreSlim(1, 1));

            await fileLock.WaitAsync();
            try
            {
                await SaveFileAsyncInternal(targetFilePath, content);
            }
            finally
            {
                fileLock.Release();
            }
        }

        internal static void RestoreFile(string targetFilePath, byte[] content)
        {
            targetFilePath = ValidateTargetFilePath(targetFilePath);
            var fileLock = _fileLocks.GetOrAdd(targetFilePath, _ => new SemaphoreSlim(1, 1));

            fileLock.Wait();
            try
            {
                string tempFilePath = GetTempFilePath(targetFilePath);
                try
                {
                    using (
                        FileStream stream = new(
                            tempFilePath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            4096,
                            FileOptions.WriteThrough
                        )
                    )
                    {
                        stream.Write(content);
                        stream.Flush(true);
                    }
                    ReplaceWithoutOverwritingBackup(tempFilePath, targetFilePath);
                }
                catch
                {
                    DeleteIfExists(tempFilePath);
                    throw;
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        private static async Task SaveFileAsyncInternal(string targetFilePath, byte[] content)
        {
            targetFilePath = ValidateTargetFilePath(targetFilePath);
            string tempFilePath = GetTempFilePath(targetFilePath);

            string backupFilePath = ValidateTargetFilePath($"{targetFilePath}.bak");

            try
            {
                await using (
                    FileStream stream = new(
                        tempFilePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        4096,
                        FileOptions.Asynchronous | FileOptions.WriteThrough
                    )
                )
                {
                    await stream.WriteAsync(content);
                    await stream.FlushAsync();
                    stream.Flush(true);
                }

                if (File.Exists(targetFilePath))
                {
                    File.Replace(tempFilePath, targetFilePath, backupFilePath);
                }
                else
                {
                    File.Move(tempFilePath, targetFilePath);
                }
            }
            catch
            {
                DeleteIfExists(tempFilePath);
                throw;
            }
        }

        private static string GetTempFilePath(string targetFilePath)
        {
            targetFilePath = ValidateTargetFilePath(targetFilePath);
            string directory =
                Path.GetDirectoryName(targetFilePath)
                ?? throw new ArgumentException("Invalid file path");
            string fileName = Path.GetFileName(targetFilePath);
            return Path.Combine(directory, $"{fileName}.{Guid.NewGuid():N}.tmp");
        }

        private static void ReplaceWithoutOverwritingBackup(
            string tempFilePath,
            string targetFilePath
        )
        {
            tempFilePath = ValidateTargetFilePath(tempFilePath);
            targetFilePath = ValidateTargetFilePath(targetFilePath);
            if (!File.Exists(targetFilePath))
            {
                File.Move(tempFilePath, targetFilePath);
                return;
            }

            string displacedFilePath = GetTempFilePath(targetFilePath);
            try
            {
                File.Replace(tempFilePath, targetFilePath, displacedFilePath);
            }
            finally
            {
                DeleteIfExists(displacedFilePath);
            }
        }

        private static void DeleteIfExists(string path)
        {
            path = ValidateTargetFilePath(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string ValidateTargetFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Contains("..", StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid file path");
            }

            string appDataDirectory = Path.GetFullPath(
                InstallationContext.GetAppDataFolderPath()
            );
            string fullPath = Path.GetFullPath(path);
            string relativePath = Path.GetRelativePath(appDataDirectory, fullPath);
            if (
                Path.IsPathRooted(relativePath)
                || relativePath.Equals("..", StringComparison.Ordinal)
                || relativePath.StartsWith(
                    $"..{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
                || relativePath.StartsWith(
                    $"..{Path.AltDirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
            )
            {
                throw new ArgumentException("File path must be inside the application data folder");
            }
            return fullPath;
        }
    }
}
