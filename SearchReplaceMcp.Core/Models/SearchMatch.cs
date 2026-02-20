namespace SearchReplaceMcp.Core.Models
{
    /// <summary>
    /// Represents a single search match within a file.
    /// </summary>
    /// <param name="FilePath">The relative file path (forward slashes).</param>
    /// <param name="LineNumber">The 1-based line number of the match.</param>
    /// <param name="LineContent">The full content of the matched line.</param>
    /// <param name="MatchStartIndex">The 0-based start index of the match within the line.</param>
    /// <param name="MatchLength">The length of the matched text.</param>
    /// <param name="MatchedText">The actual text that was matched.</param>
    public record SearchMatch(
        string FilePath,
        int LineNumber,
        string LineContent,
        int MatchStartIndex,
        int MatchLength,
        string MatchedText);
}
