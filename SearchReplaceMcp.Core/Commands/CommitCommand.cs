using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using System.Text;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to apply staged replacements to files.
    /// </summary>
    public class CommitCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;
        private readonly IFileOperationService _fileOperationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="fileOperationService">The file operation service.</param>
        public CommitCommand(ISessionManager sessionManager, IFileOperationService fileOperationService)
        {
            _sessionManager = sessionManager;
            _fileOperationService = fileOperationService;
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

                if (session.StagedReplacements.Count == 0)
                {
                    return new CommandResult(false, "Error: No staged replacements to commit.");
                }

                int filesModified = 0;
                int replacementsApplied = 0;
                List<string> modifiedFiles = new List<string>();
                List<string> skippedFiles = new List<string>();

                // Group replacements by file
                var fileGroups = session.StagedReplacements.GroupBy(r => r.FilePath);

                foreach (var fileGroup in fileGroups)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string[] lines = await _fileOperationService.ReadFileLinesAsync(
                        fileGroup.Key, session.BasePath, cancellationToken);

                    // Check for stale lines - if any line has changed since staging, skip the entire file
                    bool isStale = false;
                    foreach (StagedReplacement replacement in fileGroup)
                    {
                        int lineIndex = replacement.LineNumber - 1;
                        if (lineIndex < 0 || lineIndex >= lines.Length || lines[lineIndex] != replacement.OriginalLine)
                        {
                            isStale = true;
                            break;
                        }
                    }

                    if (isStale)
                    {
                        skippedFiles.Add(fileGroup.Key);
                        continue;
                    }

                    // Group replacements by line number
                    var lineGroups = fileGroup.GroupBy(r => r.LineNumber);
                    int fileReplacements = 0;

                    foreach (var lineGroup in lineGroups)
                    {
                        int lineIndex = lineGroup.Key - 1; // Convert 1-based to 0-based

                        // Sort by MatchStartIndex descending to apply right-to-left
                        var sorted = lineGroup.OrderByDescending(r => r.MatchStartIndex).ToList();

                        StringBuilder sb = new StringBuilder(lines[lineIndex]);
                        foreach (StagedReplacement replacement in sorted)
                        {
                            sb.Remove(replacement.MatchStartIndex, replacement.MatchLength);
                            sb.Insert(replacement.MatchStartIndex, replacement.ReplacementText);
                            fileReplacements++;
                        }

                        lines[lineIndex] = sb.ToString();
                    }

                    await _fileOperationService.WriteFileLinesAsync(
                        fileGroup.Key, session.BasePath, lines, cancellationToken);

                    filesModified++;
                    replacementsApplied += fileReplacements;
                    modifiedFiles.Add(fileGroup.Key);
                }

                // Clear session after commit
                await _sessionManager.CancelSessionAsync(cancellationToken);

                // Build result message
                List<string> detailLines = new List<string>();
                foreach (string file in modifiedFiles)
                {
                    detailLines.Add($"  {file}");
                }
                foreach (string file in skippedFiles)
                {
                    detailLines.Add($"  {file} (skipped - file changed since staging)");
                }
                string details = string.Join("\n", detailLines);

                if (skippedFiles.Count > 0)
                {
                    return new CommandResult(true,
                        $"Committed {replacementsApplied} replacement(s) in {filesModified} file(s). " +
                        $"Skipped {skippedFiles.Count} file(s) due to external changes. Session cleared.",
                        details);
                }

                return new CommandResult(true, $"Committed {replacementsApplied} replacement(s) in {filesModified} file(s). Session cleared.", details);
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
            catch (IOException ex)
            {
                return new CommandResult(false, $"Error writing file: {ex.Message}");
            }
        }
    }
}
