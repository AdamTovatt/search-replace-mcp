using SearchReplaceMcp.Core.Interfaces;

namespace SearchReplaceMcp.Core.Services
{
    /// <summary>
    /// Service for performing file read/write operations using System.IO.
    /// </summary>
    public class FileOperationService : IFileOperationService
    {
        /// <inheritdoc/>
        public async Task<string[]> ReadFileLinesAsync(string relativePath, string basePath, CancellationToken cancellationToken)
        {
            string fullPath = GetFullPath(relativePath, basePath);
            return await File.ReadAllLinesAsync(fullPath, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WriteFileLinesAsync(string relativePath, string basePath, string[] lines, CancellationToken cancellationToken)
        {
            string fullPath = GetFullPath(relativePath, basePath);

            // Detect original line ending style and trailing newline
            string originalContent = await File.ReadAllTextAsync(fullPath, cancellationToken);
            string lineEnding = DetectLineEnding(originalContent);
            bool hasTrailingNewline = originalContent.Length > 0 &&
                (originalContent.EndsWith('\n') || originalContent.EndsWith('\r'));

            string content = string.Join(lineEnding, lines);
            if (hasTrailingNewline)
            {
                content += lineEnding;
            }

            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> FileExistsAsync(string relativePath, string basePath, CancellationToken cancellationToken)
        {
            string fullPath = GetFullPath(relativePath, basePath);
            return Task.FromResult(File.Exists(fullPath));
        }

        /// <inheritdoc/>
        public async Task<bool> IsBinaryFileAsync(string relativePath, string basePath, CancellationToken cancellationToken)
        {
            string fullPath = GetFullPath(relativePath, basePath);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                byte[] buffer = new byte[8192];
                using FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (IOException)
            {
                // If we can't read the file, treat it as binary (skip it)
                return true;
            }
        }

        /// <summary>
        /// Detects the line ending style used in the file content.
        /// </summary>
        public static string DetectLineEnding(string content)
        {
            int crlfCount = 0;
            int lfCount = 0;

            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    crlfCount++;
                    i++; // Skip the \n
                }
                else if (content[i] == '\n')
                {
                    lfCount++;
                }
            }

            // Use whichever style is more common, default to \n
            return crlfCount >= lfCount && crlfCount > 0 ? "\r\n" : "\n";
        }

        private static string GetFullPath(string relativePath, string basePath)
        {
            // Convert forward slashes to platform-specific separator
            string normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(basePath, normalizedRelative);
        }
    }
}
