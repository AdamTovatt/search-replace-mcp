using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Interfaces
{
    /// <summary>
    /// Manages search-and-replace sessions.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Gets the currently active session, if one exists.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The active session, or null if no session exists.</returns>
        Task<Session?> GetActiveSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new session with the specified base path.
        /// </summary>
        /// <param name="basePath">The base directory path for file operations.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The newly created session.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a session already exists.</exception>
        Task<Session> CreateSessionAsync(string basePath, CancellationToken cancellationToken);

        /// <summary>
        /// Adds document paths to the active session.
        /// </summary>
        /// <param name="documentPaths">The relative document paths to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if no active session exists.</exception>
        Task AddDocumentsAsync(List<string> documentPaths, CancellationToken cancellationToken);

        /// <summary>
        /// Adds staged replacements to the active session.
        /// </summary>
        /// <param name="replacements">The replacements to stage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if no active session exists.</exception>
        Task AddReplacementsAsync(List<StagedReplacement> replacements, CancellationToken cancellationToken);

        /// <summary>
        /// Clears all staged replacements from the active session.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if no active session exists.</exception>
        Task ClearReplacementsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Cancels and removes the active session.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if no active session exists.</exception>
        Task CancelSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Validates that an active session exists.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if a session exists, false otherwise.</returns>
        Task<bool> HasActiveSessionAsync(CancellationToken cancellationToken);
    }
}
