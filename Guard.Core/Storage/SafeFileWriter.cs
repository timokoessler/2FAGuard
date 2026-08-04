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
            string tempFilePath = GetTempFilePath(targetFilePath);

            string backupFilePath = $"{targetFilePath}.bak";

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
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
