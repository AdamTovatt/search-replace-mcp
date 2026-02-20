using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Interfaces
{
    /// <summary>
    /// Service for searching files and staging replacements.
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Searches the specified files for matches.
        /// </summary>
        /// <param name="filePaths">The relative file paths to search.</param>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="options">Search options.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of search matches.</returns>
        Task<List<SearchMatch>> SearchAsync(
            List<string> filePaths,
            string basePath,
            string pattern,
            SearchOptions options,
            CancellationToken cancellationToken);

        /// <summary>
        /// Searches the specified files and creates staged replacements.
        /// </summary>
        /// <param name="filePaths">The relative file paths to search.</param>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="replacement">The replacement text.</param>
        /// <param name="options">Search options.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of staged replacements.</returns>
        Task<List<StagedReplacement>> SearchAndStageReplacementsAsync(
            List<string> filePaths,
            string basePath,
            string pattern,
            string replacement,
            SearchOptions options,
            CancellationToken cancellationToken);
    }
}
