using SearchReplaceMcp.Core.Interfaces;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to cancel the current session and discard all staged replacements.
    /// </summary>
    public class CancelCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public CancelCommand(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _sessionManager.CancelSessionAsync(cancellationToken);
                return new CommandResult(true, "Session cancelled. All staged replacements discarded.");
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
        }
    }
}
