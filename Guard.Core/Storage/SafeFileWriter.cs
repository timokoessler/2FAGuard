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

        private static async Task SaveFileAsyncInternal(string targetFilePath, byte[] content)
        {
            string directory =
                Path.GetDirectoryName(targetFilePath)
                ?? throw new ArgumentException("Invalid file path");
            string fileName = Path.GetFileName(targetFilePath);

            string randomPart = Guid.NewGuid().ToString("N");
            string tempFileName = $"{fileName}.{randomPart}.tmp";
            string tempFilePath = Path.Combine(directory, tempFileName);

            string backupFilePath = Path.Combine(directory, $"{fileName}.bak");

            try
            {
                // Write to temp file asynchronously
                await File.WriteAllBytesAsync(tempFilePath, content);

                if (File.Exists(targetFilePath))
                {
                    // Replace original file atomically and create backup with .bak extension
                    File.Replace(tempFilePath, targetFilePath, backupFilePath);
                }
                else
                {
                    // No original file, just move temp to target
                    File.Move(tempFilePath, targetFilePath);
                }
            }
            catch
            {
                // Clean up temp file if something went wrong
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                throw;
            }
        }
    }
}
