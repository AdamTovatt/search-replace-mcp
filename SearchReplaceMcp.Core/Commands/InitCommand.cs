using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to initialize a new search-and-replace session.
    /// </summary>
    public class InitCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;
        private readonly string _basePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="basePath">The base directory path.</param>
        public InitCommand(ISessionManager sessionManager, string basePath)
        {
            _sessionManager = sessionManager;
            _basePath = basePath;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Session session = await _sessionManager.CreateSessionAsync(_basePath, cancellationToken);
                string message = $"Session initialized at: {session.BasePath}";
                return new CommandResult(true, message);
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
            catch (DirectoryNotFoundException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
        }
    }
}
