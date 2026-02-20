using SearchReplaceMcp.Core.Interfaces;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to display help information.
    /// </summary>
    public class HelpCommand : ICommand
    {
        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            string helpText = @"SearchReplaceMcp - Search and replace tool with staging

Usage:
  sr init [path]                    Initialize a session at the specified path (default: current directory)
  sr add <path1> [path2] ...        Add documents to scope (files, directories, or glob patterns)
  sr search <pattern> [options]     Search added documents for matches
  sr replace <pattern> <replacement> [options]  Search and stage replacements
  sr preview                        Preview all staged replacements
  sr commit                         Apply all staged replacements to files
  sr cancel                         Discard the current session
  sr help                           Display this help message

Search/Replace Options:
  -c, --match-case       Case-sensitive matching
  -w, --whole-word       Match whole words only
  -r, --regex            Treat pattern as regular expression
  -p, --preserve-case    Preserve the case pattern of matched text (replace only)

MCP Server Mode:
  sr --mcp               Start as MCP server (stdio transport)

Examples:
  sr init .
  sr add ""**/*.cs""
  sr search ""oldMethod"" -c
  sr replace ""oldMethod"" ""newMethod"" -c -w
  sr preview
  sr commit";

            return Task.FromResult(new CommandResult(true, helpText));
        }
    }
}
