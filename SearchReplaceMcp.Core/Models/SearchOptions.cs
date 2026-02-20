namespace SearchReplaceMcp.Core.Models
{
    /// <summary>
    /// Options that control search behavior.
    /// </summary>
    /// <param name="MatchCase">Whether the search is case-sensitive. Default false.</param>
    /// <param name="MatchWholeWord">Whether to match whole words only. Default false.</param>
    /// <param name="UseRegex">Whether the search pattern is a regular expression. Default false.</param>
    /// <param name="PreserveCase">Whether to preserve the case pattern of matched text in replacements. Default false.</param>
    public record SearchOptions(
        bool MatchCase = false,
        bool MatchWholeWord = false,
        bool UseRegex = false,
        bool PreserveCase = false);
}
