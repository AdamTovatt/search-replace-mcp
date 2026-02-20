using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace SearchReplaceMcp.Cli
{
    /// <summary>
    /// MCP tools for search-and-replace operations.
    /// </summary>
    [McpServerToolType]
    public class McpTools
    {
        private readonly ISessionManager _sessionManager;
        private readonly ISearchService _searchService;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDocumentResolver _documentResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpTools"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="searchService">The search service.</param>
        /// <param name="fileOperationService">The file operation service.</param>
        /// <param name="documentResolver">The document resolver.</param>
        public McpTools(
            ISessionManager sessionManager,
            ISearchService searchService,
            IFileOperationService fileOperationService,
            IDocumentResolver documentResolver)
        {
            _sessionManager = sessionManager;
            _searchService = searchService;
            _fileOperationService = fileOperationService;
            _documentResolver = documentResolver;
        }

        /// <summary>
        /// Initialize a new search-and-replace session.
        /// </summary>
        /// <param name="path">The base directory path. If not provided, uses current directory.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        [McpServerTool(Name = "sr_init")]
        [Description("Initialize a new search-and-replace session. Workflow: sr_init → sr_add (scope files) → sr_search or sr_replace → sr_preview → sr_commit. Call sr_help for full usage details.")]
        public async Task<string> InitializeSessionAsync(
            [Description("The base directory path (optional, defaults to current directory)")]
            string? path,
            CancellationToken cancellationToken)
        {
            string basePath = string.IsNullOrEmpty(path)
                ? Directory.GetCurrentDirectory()
                : path;

            InitCommand command = new InitCommand(_sessionManager, basePath);
            CommandResult result = await command.ExecuteAsync(cancellationToken);

            return FormatResult(result);
        }

        /// <summary>
        /// Add documents to the session scope.
        /// </summary>
        /// <param name="paths">Comma-separated list of paths (files, directories, or glob patterns).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        [McpServerTool(Name = "sr_add")]
        [Description("Add documents to the session scope. Must be called after sr_init and before sr_search/sr_replace. Accepts files, directories, or glob patterns. Can be called multiple times - duplicates are ignored.")]
        public async Task<string> AddDocumentsAsync(
            [Description("Comma-separated list of paths to add (files, directories, or glob patterns like '**/*.cs')")]
            string paths,
            CancellationToken cancellationToken)
        {
            List<string> pathList = paths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            AddCommand command = new AddCommand(_sessionManager, _documentResolver, pathList);
            CommandResult result = await command.ExecuteAsync(cancellationToken);

            return FormatResult(result);
        }

        /// <summary>
        /// Search added documents for matches.
        /// </summary>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="matchCase">Whether to match case. Default false.</param>
        /// <param name="wholeWord">Whether to match whole words only. Default false.</param>
        /// <param name="useRegex">Whether the pattern is a regular expression. Default false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The search results.</returns>
        [McpServerTool(Name = "sr_search")]
        [Description("Search added documents for matches without staging any replacements. Use sr_replace instead if you want to stage replacements.")]
        public async Task<string> SearchAsync(
            [Description("The search pattern")]
            string pattern,
            [Description("Case-sensitive matching (default: false)")]
            bool matchCase,
            [Description("Match whole words only (default: false)")]
            bool wholeWord,
            [Description("Treat pattern as regular expression (default: false)")]
            bool useRegex,
            CancellationToken cancellationToken)
        {
            SearchOptions options = new SearchOptions(matchCase, wholeWord, useRegex);
            SearchCommand command = new SearchCommand(_sessionManager, _searchService, pattern, options);
            CommandResult result = await command.ExecuteAsync(cancellationToken);

            return FormatResult(result);
        }

        /// <summary>
        /// Search and stage replacements.
        /// </summary>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="replacement">The replacement text.</param>
        /// <param name="matchCase">Whether to match case. Default false.</param>
        /// <param name="wholeWord">Whether to match whole words only. Default false.</param>
        /// <param name="useRegex">Whether the pattern is a regular expression. Default false.</param>
        /// <param name="preserveCase">Whether to preserve case pattern. Default false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        [McpServerTool(Name = "sr_replace")]
        [Description("Search and stage replacements. Does not modify files yet - use sr_preview to review changes, then sr_commit to apply them. Can be called multiple times with different patterns; overlapping replacements are automatically skipped.")]
        public async Task<string> ReplaceAsync(
            [Description("The search pattern")]
            string pattern,
            [Description("The replacement text")]
            string replacement,
            [Description("Case-sensitive matching (default: false)")]
            bool matchCase,
            [Description("Match whole words only (default: false)")]
            bool wholeWord,
            [Description("Treat pattern as regular expression (default: false)")]
            bool useRegex,
            [Description("Preserve the case pattern of matched text in replacements (default: false)")]
            bool preserveCase,
            CancellationToken cancellationToken)
        {
            SearchOptions options = new SearchOptions(matchCase, wholeWord, useRegex, preserveCase);
            ReplaceCommand command = new ReplaceCommand(_sessionManager, _searchService, pattern, replacement, options);
            CommandResult result = await command.ExecuteAsync(cancellationToken);

            return FormatResult(result);
        }

        /// <summary>
        /// Preview all staged replacements.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The preview of staged replacements.</returns>
        [McpServerTool(Name = "sr_preview")]
        [Description("Preview all staged replacements showing before/after for each affected line")]
        public async Task<string> PreviewAsync(CancellationToken cancellationToken)
        {
            PreviewCommand command = new PreviewCommand(_sessionManager);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        /// <summary>
        /// Commit (apply) all staged replacements to files.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the commit operation.</returns>
        [McpServerTool(Name = "sr_commit")]
        [Description("Apply all staged replacements to files and clear the session")]
        public async Task<string> CommitAsync(CancellationToken cancellationToken)
        {
            CommitCommand command = new CommitCommand(_sessionManager, _fileOperationService);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        /// <summary>
        /// Cancel the current session and discard all staged replacements.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the cancel operation.</returns>
        [McpServerTool(Name = "sr_cancel")]
        [Description("Cancel the current session and discard all staged replacements")]
        public async Task<string> CancelSessionAsync(CancellationToken cancellationToken)
        {
            CancelCommand command = new CancelCommand(_sessionManager);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        /// <summary>
        /// Get help information about the SearchReplace tool.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Help text.</returns>
        [McpServerTool(Name = "sr_help")]
        [Description("Get help information about the SearchReplace tool including full workflow, all commands, and option descriptions. Call this first if unsure how to use the tool.")]
        public async Task<string> GetHelpAsync(CancellationToken cancellationToken)
        {
            HelpCommand command = new HelpCommand();
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        private static string FormatResult(CommandResult result)
        {
            if (string.IsNullOrEmpty(result.Details))
            {
                return result.Message;
            }

            return $"{result.Message}\n\n{result.Details}";
        }
    }
}
