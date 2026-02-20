using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to search and stage replacements.
    /// </summary>
    public class ReplaceCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;
        private readonly ISearchService _searchService;
        private readonly string _pattern;
        private readonly string _replacement;
        private readonly SearchOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplaceCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="searchService">The search service.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="replacement">The replacement text.</param>
        /// <param name="options">Search options.</param>
        public ReplaceCommand(
            ISessionManager sessionManager,
            ISearchService searchService,
            string pattern,
            string replacement,
            SearchOptions options)
        {
            _sessionManager = sessionManager;
            _searchService = searchService;
            _pattern = pattern;
            _replacement = replacement;
            _options = options;
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

                if (session.AddedDocuments.Count == 0)
                {
                    return new CommandResult(false, "Error: No documents in scope. Run 'sr add' first.");
                }

                List<StagedReplacement> replacements = await _searchService.SearchAndStageReplacementsAsync(
                    session.AddedDocuments, session.BasePath, _pattern, _replacement, _options, cancellationToken);

                if (replacements.Count == 0)
                {
                    return new CommandResult(true, "No matches found. Nothing staged.");
                }

                await _sessionManager.AddReplacementsAsync(replacements, cancellationToken);

                int fileCount = replacements.Select(r => r.FilePath).Distinct().Count();
                string details = FormatReplacements(replacements);
                return new CommandResult(true, $"Staged {replacements.Count} replacement(s) in {fileCount} file(s). Use 'sr preview' to review or 'sr commit' to apply.", details);
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
        }

        private static string FormatReplacements(List<StagedReplacement> replacements)
        {
            var grouped = replacements.GroupBy(r => r.FilePath);
            List<string> lines = new List<string>();

            foreach (var group in grouped)
            {
                lines.Add($"  {group.Key}:");
                foreach (StagedReplacement replacement in group)
                {
                    lines.Add($"    Line {replacement.LineNumber}: '{replacement.OriginalLine.TrimEnd()}'");
                }
            }

            return string.Join("\n", lines);
        }
    }
}
