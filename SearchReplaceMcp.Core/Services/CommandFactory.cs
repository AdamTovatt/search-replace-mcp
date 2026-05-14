using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Core.Services
{
    /// <summary>
    /// Factory for creating command instances from command-line arguments.
    /// </summary>
    public class CommandFactory : ICommandFactory
    {
        private readonly ISessionManager _sessionManager;
        private readonly ISearchService _searchService;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDocumentResolver _documentResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandFactory"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="searchService">The search service.</param>
        /// <param name="fileOperationService">The file operation service.</param>
        /// <param name="documentResolver">The document resolver.</param>
        public CommandFactory(
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

        /// <inheritdoc/>
        public ICommand CreateCommand(string[] args)
        {
            if (args.Length == 0)
            {
                return new HelpCommand();
            }

            string commandName = args[0].ToLowerInvariant();

            return commandName switch
            {
                "init" => CreateInitCommand(args),
                "add" => CreateAddCommand(args),
                "search" => CreateSearchCommand(args),
                "replace" => CreateReplaceCommand(args),
                "preview" => new PreviewCommand(_sessionManager),
                "commit" => new CommitCommand(_sessionManager, _fileOperationService),
                "cancel" => new CancelCommand(_sessionManager),
                "help" or "--help" or "-h" => new HelpCommand(),
                _ => throw new ArgumentException(
                    $"Unknown command: '{commandName}'. Use 'sr help' to see available commands.")
            };
        }

        private ICommand CreateInitCommand(string[] args)
        {
            // sr init [path]
            string basePath;

            if (args.Length > 1)
            {
                basePath = args[1];
            }
            else
            {
                basePath = Directory.GetCurrentDirectory();
            }

            return new InitCommand(_sessionManager, basePath);
        }

        private ICommand CreateAddCommand(string[] args)
        {
            // sr add <path1> [path2] ...
            if (args.Length < 2)
            {
                throw new ArgumentException(
                    "Add command requires at least one path. " +
                    "Usage: sr add <path1> [path2] ...");
            }

            List<string> paths = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                paths.Add(args[i]);
            }

            return new AddCommand(_sessionManager, _documentResolver, paths);
        }

        private ICommand CreateSearchCommand(string[] args)
        {
            // sr search <pattern> [-c|--match-case] [-w|--whole-word] [-r|--regex] [--no-ecma]
            if (args.Length < 2)
            {
                throw new ArgumentException(
                    "Search command requires a pattern. " +
                    "Usage: sr search <pattern> [-c|--match-case] [-w|--whole-word] [-r|--regex] [--no-ecma]");
            }

            string pattern = args[1];
            SearchOptions options = ParseSearchOptions(args, 2);

            return new SearchCommand(_sessionManager, _searchService, pattern, options);
        }

        private ICommand CreateReplaceCommand(string[] args)
        {
            // sr replace <pattern> <replacement> [-c|--match-case] [-w|--whole-word] [-r|--regex] [-p|--preserve-case] [--no-ecma]
            if (args.Length < 3)
            {
                throw new ArgumentException(
                    "Replace command requires a pattern and replacement. " +
                    "Usage: sr replace <pattern> <replacement> [-c|--match-case] [-w|--whole-word] [-r|--regex] [-p|--preserve-case] [--no-ecma]");
            }

            string pattern = args[1];
            string replacement = args[2];
            SearchOptions options = ParseSearchOptions(args, 3);

            return new ReplaceCommand(_sessionManager, _searchService, pattern, replacement, options);
        }

        private static SearchOptions ParseSearchOptions(string[] args, int startIndex)
        {
            bool matchCase = false;
            bool matchWholeWord = false;
            bool useRegex = false;
            bool preserveCase = false;
            bool ecmaScript = true;

            for (int i = startIndex; i < args.Length; i++)
            {
                string flag = args[i].ToLowerInvariant();
                switch (flag)
                {
                    case "-c":
                    case "--match-case":
                        matchCase = true;
                        break;
                    case "-w":
                    case "--whole-word":
                        matchWholeWord = true;
                        break;
                    case "-r":
                    case "--regex":
                        useRegex = true;
                        break;
                    case "-p":
                    case "--preserve-case":
                        preserveCase = true;
                        break;
                    case "--no-ecma":
                        ecmaScript = false;
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unknown option: '{args[i]}'. Use 'sr help' to see available options.");
                }
            }

            return new SearchOptions(matchCase, matchWholeWord, useRegex, preserveCase, ecmaScript);
        }
    }
}
