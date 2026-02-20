using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using System.Text;

namespace SearchReplaceMcp.Core.Commands
{
    /// <summary>
    /// Command to preview staged replacements.
    /// </summary>
    public class PreviewCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewCommand"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PreviewCommand(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
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
                    return new CommandResult(true, "No staged replacements.", $"Session: {session.BasePath}\nDocuments in scope: {session.AddedDocuments.Count}");
                }

                string details = FormatPreview(session);
                int fileCount = session.StagedReplacements.Select(r => r.FilePath).Distinct().Count();
                return new CommandResult(true, $"{session.StagedReplacements.Count} staged replacement(s) in {fileCount} file(s).", details);
            }
            catch (InvalidOperationException ex)
            {
                return new CommandResult(false, "Error: " + ex.Message);
            }
        }

        private static string FormatPreview(Session session)
        {
            List<string> lines = new List<string>();
            lines.Add($"Session: {session.BasePath}");
            lines.Add($"Documents in scope: {session.AddedDocuments.Count}");
            lines.Add("");

            var grouped = session.StagedReplacements.GroupBy(r => r.FilePath);

            foreach (var fileGroup in grouped)
            {
                lines.Add($"  {fileGroup.Key}:");

                // Group by line number to show the preview for each line
                var lineGroups = fileGroup.GroupBy(r => r.LineNumber).OrderBy(g => g.Key);

                foreach (var lineGroup in lineGroups)
                {
                    StagedReplacement first = lineGroup.First();
                    string previewLine = ApplyReplacementsToLine(first.OriginalLine, lineGroup.ToList());

                    lines.Add($"    Line {lineGroup.Key}:");
                    lines.Add($"      - {first.OriginalLine.TrimEnd()}");
                    lines.Add($"      + {previewLine.TrimEnd()}");
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Applies all replacements for a single line by processing right-to-left.
        /// </summary>
        public static string ApplyReplacementsToLine(string originalLine, List<StagedReplacement> replacements)
        {
            // Sort by MatchStartIndex descending to apply right-to-left
            var sorted = replacements.OrderByDescending(r => r.MatchStartIndex).ToList();

            StringBuilder sb = new StringBuilder(originalLine);
            foreach (StagedReplacement replacement in sorted)
            {
                sb.Remove(replacement.MatchStartIndex, replacement.MatchLength);
                sb.Insert(replacement.MatchStartIndex, replacement.ReplacementText);
            }

            return sb.ToString();
        }
    }
}
