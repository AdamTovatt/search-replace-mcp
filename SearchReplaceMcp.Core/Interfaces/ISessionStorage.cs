using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Interfaces
{
    /// <summary>
    /// Handles persistence of session data.
    /// </summary>
    public interface ISessionStorage
    {
        /// <summary>
        /// Loads the active session from storage.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The session, or null if no session exists.</returns>
        Task<Session?> LoadSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Saves a session to storage.
        /// </summary>
        /// <param name="session">The session to save.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task SaveSessionAsync(Session session, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the active session from storage.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task DeleteSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a session exists in storage.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if a session exists, false otherwise.</returns>
        Task<bool> SessionExistsAsync(CancellationToken cancellationToken);
    }
}
