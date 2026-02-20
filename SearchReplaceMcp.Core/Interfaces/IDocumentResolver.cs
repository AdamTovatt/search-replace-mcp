namespace SearchReplaceMcp.Core.Interfaces
{
    /// <summary>
    /// Resolves file paths from globs, directories, and individual file paths.
    /// </summary>
    public interface IDocumentResolver
    {
        /// <summary>
        /// Resolves the specified paths to a list of relative file paths.
        /// Supports glob patterns, directory paths (recursed), and individual file paths.
        /// Skips binary files and deduplicates results.
        /// </summary>
        /// <param name="paths">The paths to resolve (globs, directories, or files).</param>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A deduplicated list of relative file paths with forward slashes.</returns>
        Task<List<string>> ResolveAsync(List<string> paths, string basePath, CancellationToken cancellationToken);
    }
}
