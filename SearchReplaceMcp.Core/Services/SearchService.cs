using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace SearchReplaceMcp.Core.Services
{
    /// <summary>
    /// Regex-based search engine with preserve-case support.
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly IFileOperationService _fileOperationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchService"/> class.
        /// </summary>
        /// <param name="fileOperationService">The file operation service.</param>
        public SearchService(IFileOperationService fileOperationService)
        {
            _fileOperationService = fileOperationService;
        }

        /// <inheritdoc/>
        public async Task<List<SearchMatch>> SearchAsync(
            List<string> filePaths,
            string basePath,
            string pattern,
            SearchOptions options,
            CancellationToken cancellationToken)
        {
            Regex regex = BuildRegex(pattern, options);
            List<SearchMatch> matches = new List<SearchMatch>();

            foreach (string filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string[] lines = await _fileOperationService.ReadFileLinesAsync(filePath, basePath, cancellationToken);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        MatchCollection lineMatches = regex.Matches(lines[i]);
                        foreach (Match match in lineMatches)
                        {
                            matches.Add(new SearchMatch(
                                FilePath: filePath,
                                LineNumber: i + 1,
                                LineContent: lines[i],
                                MatchStartIndex: match.Index,
                                MatchLength: match.Length,
                                MatchedText: match.Value));
                        }
                    }
                }
                catch (IOException)
                {
                    // Skip files that can't be read
                }
            }

            return matches;
        }

        /// <inheritdoc/>
        public async Task<List<StagedReplacement>> SearchAndStageReplacementsAsync(
            List<string> filePaths,
            string basePath,
            string pattern,
            string replacement,
            SearchOptions options,
            CancellationToken cancellationToken)
        {
            Regex regex = BuildRegex(pattern, options);
            List<StagedReplacement> replacements = new List<StagedReplacement>();

            foreach (string filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string[] lines = await _fileOperationService.ReadFileLinesAsync(filePath, basePath, cancellationToken);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        MatchCollection lineMatches = regex.Matches(lines[i]);
                        foreach (Match match in lineMatches)
                        {
                            string replacementText = options.UseRegex
                                ? match.Result(replacement)
                                : replacement;

                            if (options.PreserveCase)
                            {
                                replacementText = ApplyPreserveCase(match.Value, replacementText);
                            }

                            replacements.Add(new StagedReplacement(
                                FilePath: filePath,
                                LineNumber: i + 1,
                                OriginalLine: lines[i],
                                MatchStartIndex: match.Index,
                                MatchLength: match.Length,
                                ReplacementText: replacementText));
                        }
                    }
                }
                catch (IOException)
                {
                    // Skip files that can't be read
                }
            }

            return replacements;
        }

        /// <summary>
        /// Builds a regex from the search pattern and options.
        /// </summary>
        public static Regex BuildRegex(string pattern, SearchOptions options)
        {
            string regexPattern;

            if (options.UseRegex)
            {
                regexPattern = pattern;
            }
            else
            {
                regexPattern = Regex.Escape(pattern);
            }

            if (options.MatchWholeWord)
            {
                regexPattern = $@"\b{regexPattern}\b";
            }

            RegexOptions regexOptions = RegexOptions.Compiled;
            if (!options.MatchCase)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            // ECMAScript mode lets backreferences to non-participating groups match empty
            // (e.g. `(a)(b)?\2` succeeds against "a"). Default .NET regex fails the whole match.
            if (options.UseRegex && options.EcmaScript)
            {
                regexOptions |= RegexOptions.ECMAScript;
            }

            return new Regex(regexPattern, regexOptions);
        }

        /// <summary>
        /// Applies preserve-case logic: ALL UPPER → upper, all lower → lower, Title Case → title, else → as-is.
        /// </summary>
        public static string ApplyPreserveCase(string matchedText, string replacement)
        {
            if (string.IsNullOrEmpty(matchedText) || string.IsNullOrEmpty(replacement))
            {
                return replacement;
            }

            if (IsAllUpper(matchedText))
            {
                return replacement.ToUpperInvariant();
            }

            if (IsAllLower(matchedText))
            {
                return replacement.ToLowerInvariant();
            }

            if (IsTitleCase(matchedText))
            {
                return ToTitleCase(replacement);
            }

            // No recognizable pattern, return as-is
            return replacement;
        }

        private static bool IsAllUpper(string text)
        {
            foreach (char c in text)
            {
                if (char.IsLetter(c) && !char.IsUpper(c))
                {
                    return false;
                }
            }

            return text.Any(char.IsLetter);
        }

        private static bool IsAllLower(string text)
        {
            foreach (char c in text)
            {
                if (char.IsLetter(c) && !char.IsLower(c))
                {
                    return false;
                }
            }

            return text.Any(char.IsLetter);
        }

        private static bool IsTitleCase(string text)
        {
            if (text.Length == 0 || !char.IsLetter(text[0]) || !char.IsUpper(text[0]))
            {
                return false;
            }

            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsLetter(text[i]) && !char.IsLower(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            StringBuilder sb = new StringBuilder(text.ToLowerInvariant());
            sb[0] = char.ToUpperInvariant(sb[0]);
            return sb.ToString();
        }
    }
}
