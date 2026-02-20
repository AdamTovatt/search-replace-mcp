using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Services
{
    /// <summary>
    /// Manages search-and-replace sessions.
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly ISessionStorage _sessionStorage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionManager"/> class.
        /// </summary>
        /// <param name="sessionStorage">The session storage implementation.</param>
        public SessionManager(ISessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        /// <inheritdoc/>
        public async Task<Session?> GetActiveSessionAsync(CancellationToken cancellationToken)
        {
            return await _sessionStorage.LoadSessionAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Session> CreateSessionAsync(string basePath, CancellationToken cancellationToken)
        {
            Session? existingSession = await _sessionStorage.LoadSessionAsync(cancellationToken);
            if (existingSession != null)
            {
                throw new InvalidOperationException(
                    $"Session already active at '{existingSession.BasePath}'. " +
                    "Use 'sr preview' to review or 'sr cancel' to discard.");
            }

            // Validate and normalize the base path
            string normalizedBasePath = Path.GetFullPath(basePath);
            if (!Directory.Exists(normalizedBasePath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {normalizedBasePath}");
            }

            Session session = Session.Create(normalizedBasePath);
            await _sessionStorage.SaveSessionAsync(session, cancellationToken);

            return session;
        }

        /// <inheritdoc/>
        public async Task AddDocumentsAsync(List<string> documentPaths, CancellationToken cancellationToken)
        {
            Session? session = await _sessionStorage.LoadSessionAsync(cancellationToken);
            if (session == null)
            {
                throw new InvalidOperationException(
                    "No session initialized. Run 'sr init' first.");
            }

            foreach (string path in documentPaths)
            {
                if (!session.AddedDocuments.Contains(path))
                {
                    session.AddedDocuments.Add(path);
                }
            }

            await _sessionStorage.SaveSessionAsync(session, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddReplacementsAsync(List<StagedReplacement> replacements, CancellationToken cancellationToken)
        {
            Session? session = await _sessionStorage.LoadSessionAsync(cancellationToken);
            if (session == null)
            {
                throw new InvalidOperationException(
                    "No session initialized. Run 'sr init' first.");
            }

            // Filter out replacements that overlap with already-staged ones
            foreach (StagedReplacement newReplacement in replacements)
            {
                bool overlaps = session.StagedReplacements.Any(existing =>
                    existing.FilePath == newReplacement.FilePath &&
                    existing.LineNumber == newReplacement.LineNumber &&
                    RangesOverlap(
                        existing.MatchStartIndex, existing.MatchLength,
                        newReplacement.MatchStartIndex, newReplacement.MatchLength));

                if (!overlaps)
                {
                    session.StagedReplacements.Add(newReplacement);
                }
            }

            await _sessionStorage.SaveSessionAsync(session, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ClearReplacementsAsync(CancellationToken cancellationToken)
        {
            Session? session = await _sessionStorage.LoadSessionAsync(cancellationToken);
            if (session == null)
            {
                throw new InvalidOperationException(
                    "No session initialized. Run 'sr init' first.");
            }

            session.StagedReplacements.Clear();
            await _sessionStorage.SaveSessionAsync(session, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task CancelSessionAsync(CancellationToken cancellationToken)
        {
            bool sessionExists = await _sessionStorage.SessionExistsAsync(cancellationToken);
            if (!sessionExists)
            {
                throw new InvalidOperationException(
                    "No session initialized. Run 'sr init' first.");
            }

            await _sessionStorage.DeleteSessionAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> HasActiveSessionAsync(CancellationToken cancellationToken)
        {
            return await _sessionStorage.SessionExistsAsync(cancellationToken);
        }

        private static bool RangesOverlap(int startA, int lengthA, int startB, int lengthB)
        {
            int endA = startA + lengthA;
            int endB = startB + lengthB;
            return startA < endB && startB < endA;
        }
    }
}
