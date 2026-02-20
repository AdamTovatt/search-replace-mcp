using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to search added documents for matches.
    /// </summary>
    public class SearchCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;
        private readonly ISearchService _searchService;
        private readonly string _pattern;
        private readonly SearchOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="searchService">The search service.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="options">Search options.</param>
        public SearchCommand(
            ISessionManager sessionManager,
            ISearchService searchService,
            string pattern,
            SearchOptions options)
        {
            _sessionManager = sessionManager;
            _searchService = searchService;
            _pattern = pattern;
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

                List<SearchMatch> matches = await _searchService.SearchAsync(
                    session.AddedDocuments, session.BasePath, _pattern, _options, cancellationToken);

                if (matches.Count == 0)
                {
                    return new CommandResult(true, "No matches found.");
                }

                string details = FormatMatches(matches);
                int fileCount = matches.Select(m => m.FilePath).Distinct().Count();
                return new CommandResult(true, $"Found {matches.Count} match(es) in {fileCount} file(s).", details);
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
        }

        private static string FormatMatches(List<SearchMatch> matches)
        {
            var grouped = matches.GroupBy(m => m.FilePath);
            List<string> lines = new List<string>();

            foreach (var group in grouped)
            {
                lines.Add($"  {group.Key}:");
                foreach (SearchMatch match in group)
                {
                    lines.Add($"    Line {match.LineNumber}: {match.LineContent.TrimEnd()}");
                }
            }

            return string.Join("\n", lines);
        }
    }
}
