# SearchReplaceMcp

[![Tests](https://github.com/AdamTovatt/search-replace-mcp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AdamTovatt/search-replace-mcp/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/SearchReplaceMcp.svg)](https://www.nuget.org/packages/SearchReplaceMcp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SearchReplaceMcp.svg)](https://www.nuget.org/packages/SearchReplaceMcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

A search-and-replace tool that stages replacements and applies them safely. Works as both a CLI tool and an MCP (Model Context Protocol) server for AI agents.

## Installation

```bash
dotnet tool install --global SearchReplaceMcp
```

After installation, the `sr` command will be available globally.

To update to the latest version:

```bash
dotnet tool update --global SearchReplaceMcp
```

To uninstall:

```bash
dotnet tool uninstall --global SearchReplaceMcp
```

To register it as an MCP tool in Claude Code:

```bash
claude mcp add sr -- sr --mcp
```

To use it as an MCP tool in other editors (e.g. Cursor), add this to your MCP configuration:

```json
{
  "mcpServers": {
    "searchreplace": {
      "command": "sr",
      "args": ["--mcp"]
    }
  }
}
```

## Usage

The workflow is: initialize a session, add documents, search or stage replacements, preview, then commit (or cancel).

```bash
sr init [path]                                      # Initialize at path (default: current directory)
sr add <path1> [path2] ...                          # Add files, directories, or glob patterns to scope
sr search <pattern> [options]                       # Search added documents for matches
sr replace <pattern> <replacement> [options]        # Search and stage replacements
sr preview                                          # See all staged replacements with before/after
sr commit                                           # Apply all staged replacements to files
sr cancel                                           # Discard the session
sr help                                             # Show help information
```

### Search/Replace Options

| Flag | Description |
|------|-------------|
| `-c`, `--match-case` | Case-sensitive matching |
| `-w`, `--whole-word` | Match whole words only |
| `-r`, `--regex` | Treat pattern as regular expression |
| `-p`, `--preserve-case` | Preserve the case pattern of matched text (replace only) |
| `--no-ecma` | With `--regex`, use .NET regex instead of ECMAScript. See [Regex flavor](#regex-flavor) below. |

### Examples

```bash
# Simple find and replace across C# files
sr init .
sr add "**/*.cs"
sr replace "oldMethod" "newMethod" -c -w
sr preview
sr commit

# Case-preserving rename (Hello→Goodbye, hello→goodbye, HELLO→GOODBYE)
sr init .
sr add "**/*.cs" "**/*.txt"
sr replace "hello" "goodbye" -p
sr preview
sr commit

# Regex replace with capture groups
sr init .
sr add "**/*.cs"
sr replace "Log\.Info\((.+)\)" "Logger.Information($1)" -r
sr preview
sr commit

# Search without replacing
sr init .
sr add "**/*.cs"
sr search "TODO" -c
sr cancel
```

## Behavior

### Replacements are staged before applying

Nothing is written to disk until you run `sr commit`. Use `sr preview` to review all changes before applying.

### Multiple replace calls accumulate safely

You can call `sr replace` multiple times with different patterns before committing. Overlapping replacements are automatically skipped to prevent corruption.

### Stale files are detected

If a file is modified externally between `sr replace` and `sr commit`, the file is skipped and reported rather than silently corrupted.

### Line endings are preserved

Original line ending style (LF or CRLF) is detected and preserved when writing files back.

### Preserve-case adapts to the matched text

With `-p`, the replacement text mirrors the case pattern of each match: ALL UPPER → upper, all lower → lower, Title Case → title case, mixed → as-is.

### Regex flavor

When `--regex` is used, patterns are interpreted as ECMAScript regex by default. This is what most JS/Python users expect, and — critically — it lets backreferences to non-participating groups match the empty string. For example, `(text-\w+)(?:\/(\d+))?-\2` matches both `text-red-` and `text-red/50-50` in one pass; under .NET's default regex, the first input would fail the whole match because group 2 didn't participate.

Pass `--no-ecma` to fall back to .NET regex. That mode supports look-behind (`(?<=...)`), named groups (`(?<name>...)`), balancing groups, and character class subtraction, but loses the empty-backreference behavior.

Note that ECMAScript mode also affects `\b` and the `\w` character class — both treat only ASCII letters/digits/underscore as word characters. This matters for `--whole-word` combined with `--regex` on non-ASCII text: by default the word boundary will not consider letters like `é` or `ñ` as word characters. Use `--no-ecma` if you need Unicode-aware word boundaries. Similarly, case-insensitive matching under ECMAScript mode uses simple case folding rather than .NET's culture-aware folding.

### Binary files are skipped

When adding documents, binary files (detected by null bytes in the first 8KB) are automatically excluded.

### Paths are sandboxed

All file operations are relative to the session's base directory.

### Sessions are shared

CLI and MCP mode share the same session storage, so you can initialize a session in one mode and commit in the other.

## As MCP Server

```bash
sr --mcp
```

When running as an MCP server, the following tools are available:

- `sr_init(path?: string)` - Initialize a session
- `sr_add(paths: string)` - Add documents to scope (comma-separated paths/globs)
- `sr_search(pattern, matchCase, wholeWord, useRegex, ecmaScript=true)` - Search for matches
- `sr_replace(pattern, replacement, matchCase, wholeWord, useRegex, preserveCase, ecmaScript=true)` - Stage replacements
- `sr_preview()` - Preview staged replacements
- `sr_commit()` - Apply staged replacements
- `sr_cancel()` - Cancel session
- `sr_help()` - Get help

## Development

```bash
git clone <repository-url>
cd SearchReplaceMcp
dotnet build SearchReplaceMcp.sln
dotnet test SearchReplaceMcp.sln
```

To run as MCP server during development:

```bash
dotnet run --project SearchReplaceMcp.Cli/SearchReplaceMcp.Cli.csproj -- --mcp
```

To package:

```bash
dotnet pack SearchReplaceMcp.Cli/SearchReplaceMcp.Cli.csproj --configuration Release
```

## Releasing

Releases are published to NuGet by the `Release` workflow, triggered by pushing a `v*` tag.

1. Bump `<Version>` in `SearchReplaceMcp.Cli/SearchReplaceMcp.Cli.csproj` and commit.
2. Tag the commit with the matching version, e.g. `git tag v1.2.0`.
3. Push the tag: `git push origin v1.2.0`.

The workflow verifies the tag matches the csproj version, builds, runs tests, and pushes the package to NuGet. The `NUGET_API_KEY` repository secret must be configured.

## License

MIT License
