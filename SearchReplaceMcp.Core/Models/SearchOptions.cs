namespace SearchReplaceMcp.Core.Models
{
    /// <summary>
    /// Options that control search behavior.
    /// </summary>
    /// <param name="MatchCase">Whether the search is case-sensitive. Default false.</param>
    /// <param name="MatchWholeWord">Whether to match whole words only. Default false.</param>
    /// <param name="UseRegex">Whether the search pattern is a regular expression. Default false.</param>
    /// <param name="PreserveCase">Whether to preserve the case pattern of matched text in replacements. Default false.</param>
    /// <param name="EcmaScript">When <see cref="UseRegex"/> is true, use ECMAScript regex semantics — most notably, backreferences to non-participating groups match the empty string instead of failing the whole match. Default true. Disable to fall back to .NET's default regex (enables look-behind, named groups, etc., but breaks empty-backreference matching).</param>
    public record SearchOptions(
        bool MatchCase = false,
        bool MatchWholeWord = false,
        bool UseRegex = false,
        bool PreserveCase = false,
        bool EcmaScript = true);
}
