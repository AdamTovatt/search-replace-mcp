namespace SearchReplaceMcp.Core.Models
{
    /// <summary>
    /// Represents a staged replacement to be applied to a file.
    /// </summary>
    /// <param name="FilePath">The relative file path (forward slashes).</param>
    /// <param name="LineNumber">The 1-based line number of the replacement.</param>
    /// <param name="OriginalLine">The full original content of the line.</param>
    /// <param name="MatchStartIndex">The 0-based start index of the match within the line.</param>
    /// <param name="MatchLength">The length of the matched text to replace.</param>
    /// <param name="ReplacementText">The text to replace the match with.</param>
    public record StagedReplacement(
        string FilePath,
        int LineNumber,
        string OriginalLine,
        int MatchStartIndex,
        int MatchLength,
        string ReplacementText);
}
