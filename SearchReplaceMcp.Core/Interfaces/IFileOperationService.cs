namespace SearchReplaceMcp.Core.Interfaces
{
    /// <summary>
    /// Service for performing file read/write operations.
    /// </summary>
    public interface IFileOperationService
    {
        /// <summary>
        /// Reads all lines from a file.
        /// </summary>
        /// <param name="relativePath">The relative file path.</param>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The lines of the file.</returns>
        Task<string[]> ReadFileLinesAsync(string relativePath, string basePath, CancellationToken cancellationToken);

        /// <summary>
        /// Writes lines to a file, overwriting existing content.
        /// </summary>
        /// <param name="relativePath">The relative file path.</param>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="lines">The lines to write.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task WriteFileLinesAsync(string relativePath, string basePath, string[] lines, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        /// <param name="relativePath">The relative file path.</param>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        Task<bool> FileExistsAsync(string relativePath, string basePath, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a file is binary by reading its first 8KB for null bytes.
        /// </summary>
        /// <param name="relativePath">The relative file path.</param>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if the file appears to be binary, false otherwise.</returns>
        Task<bool> IsBinaryFileAsync(string relativePath, string basePath, CancellationToken cancellationToken);
    }
}
