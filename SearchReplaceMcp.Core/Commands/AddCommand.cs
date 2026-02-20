using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to add documents to the session scope.
    /// </summary>
    public class AddCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;
        private readonly IDocumentResolver _documentResolver;
        private readonly List<string> _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="documentResolver">The document resolver.</param>
        /// <param name="paths">The paths to add (globs, directories, or files).</param>
        public AddCommand(ISessionManager sessionManager, IDocumentResolver documentResolver, List<string> paths)
        {
            _sessionManager = sessionManager;
            _documentResolver = documentResolver;
            _paths = paths;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Session? session = await _sessionManager.GetActiveSessionAsync(cancellationToken);
                if (session == null)
                {
                    return new CommandResult(false, "Error: No session initialized. Run 'sr init' first.");
                }

                List<string> resolvedPaths = await _documentResolver.ResolveAsync(_paths, session.BasePath, cancellationToken);

                if (resolvedPaths.Count == 0)
                {
                    return new CommandResult(false, "No files matched the specified paths.");
                }

                await _sessionManager.AddDocumentsAsync(resolvedPaths, cancellationToken);

                // Get updated session to report total count
                Session? updatedSession = await _sessionManager.GetActiveSessionAsync(cancellationToken);
                int totalCount = updatedSession?.AddedDocuments.Count ?? resolvedPaths.Count;

                string details = string.Join("\n", resolvedPaths.Select(p => $"  + {p}"));
                return new CommandResult(true, $"Added {resolvedPaths.Count} file(s). Total documents in scope: {totalCount}", details);
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
        }
    }
}
