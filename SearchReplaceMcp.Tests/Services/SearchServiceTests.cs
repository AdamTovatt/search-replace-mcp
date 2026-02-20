using Moq;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using SearchReplaceMcp.Core.Services;

namespace SearchReplaceMcp.Tests.Services
{
    public class SearchServiceTests
    {
        private readonly Mock<IFileOperationService> _fileOperationServiceMock;
        private readonly SearchService _searchService;

        public SearchServiceTests()
        {
            _fileOperationServiceMock = new Mock<IFileOperationService>();
            _searchService = new SearchService(_fileOperationServiceMock.Object);
        }

        [Fact]
        public async Task SearchAsync_CaseInsensitive_FindsMatches()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "Hello World", "hello there", "nothing here" });

            List<SearchMatch> matches = await _searchService.SearchAsync(
                new List<string> { "file.cs" }, "/base", "hello", new SearchOptions(), CancellationToken.None);

            Assert.Equal(2, matches.Count);
            Assert.Equal("Hello", matches[0].MatchedText);
            Assert.Equal("hello", matches[1].MatchedText);
        }

        [Fact]
        public async Task SearchAsync_CaseSensitive_OnlyFindsExactCase()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "Hello World", "hello there" });

            List<SearchMatch> matches = await _searchService.SearchAsync(
                new List<string> { "file.cs" }, "/base", "hello",
                new SearchOptions(MatchCase: true), CancellationToken.None);

            Assert.Single(matches);
            Assert.Equal("hello", matches[0].MatchedText);
            Assert.Equal(2, matches[0].LineNumber);
        }

        [Fact]
        public async Task SearchAsync_WholeWord_OnlyFindsWholeWords()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "test testing tester", "a test here" });

            List<SearchMatch> matches = await _searchService.SearchAsync(
                new List<string> { "file.cs" }, "/base", "test",
                new SearchOptions(MatchWholeWord: true), CancellationToken.None);

            Assert.Equal(2, matches.Count);
            Assert.Equal(1, matches[0].LineNumber);
            Assert.Equal(0, matches[0].MatchStartIndex);
            Assert.Equal(2, matches[1].LineNumber);
        }

        [Fact]
        public async Task SearchAsync_Regex_FindsRegexMatches()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "foo123bar", "foo456bar", "nothing" });

            List<SearchMatch> matches = await _searchService.SearchAsync(
                new List<string> { "file.cs" }, "/base", @"foo\d+bar",
                new SearchOptions(UseRegex: true), CancellationToken.None);

            Assert.Equal(2, matches.Count);
        }

        [Fact]
        public async Task SearchAsync_CorrectLineNumberAndIndex()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "first line", "second test line", "third line" });

            List<SearchMatch> matches = await _searchService.SearchAsync(
                new List<string> { "file.cs" }, "/base", "test", new SearchOptions(), CancellationToken.None);

            Assert.Single(matches);
            Assert.Equal(2, matches[0].LineNumber);
            Assert.Equal(7, matches[0].MatchStartIndex);
            Assert.Equal(4, matches[0].MatchLength);
        }

        [Fact]
        public async Task SearchAndStageReplacementsAsync_CreatesCorrectReplacements()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "hello world hello" });

            List<StagedReplacement> replacements = await _searchService.SearchAndStageReplacementsAsync(
                new List<string> { "file.cs" }, "/base", "hello", "goodbye", new SearchOptions(), CancellationToken.None);

            Assert.Equal(2, replacements.Count);
            Assert.Equal(0, replacements[0].MatchStartIndex);
            Assert.Equal(5, replacements[0].MatchLength);
            Assert.Equal("goodbye", replacements[0].ReplacementText);
            Assert.Equal(12, replacements[1].MatchStartIndex);
        }

        [Fact]
        public async Task SearchAndStageReplacementsAsync_PreserveCase_AllUpper()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "HELLO" });

            List<StagedReplacement> replacements = await _searchService.SearchAndStageReplacementsAsync(
                new List<string> { "file.cs" }, "/base", "hello", "goodbye",
                new SearchOptions(PreserveCase: true), CancellationToken.None);

            Assert.Single(replacements);
            Assert.Equal("GOODBYE", replacements[0].ReplacementText);
        }

        [Fact]
        public async Task SearchAndStageReplacementsAsync_PreserveCase_AllLower()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "hello" });

            List<StagedReplacement> replacements = await _searchService.SearchAndStageReplacementsAsync(
                new List<string> { "file.cs" }, "/base", "hello", "Goodbye",
                new SearchOptions(PreserveCase: true), CancellationToken.None);

            Assert.Single(replacements);
            Assert.Equal("goodbye", replacements[0].ReplacementText);
        }

        [Fact]
        public async Task SearchAndStageReplacementsAsync_PreserveCase_TitleCase()
        {
            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file.cs", "/base", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "Hello" });

            List<StagedReplacement> replacements = await _searchService.SearchAndStageReplacementsAsync(
                new List<string> { "file.cs" }, "/base", "hello", "goodbye",
                new SearchOptions(PreserveCase: true), CancellationToken.None);

            Assert.Single(replacements);
            Assert.Equal("Goodbye", replacements[0].ReplacementText);
        }

        [Fact]
        public void BuildRegex_PlainText_EscapesSpecialChars()
        {
            var regex = SearchService.BuildRegex("foo.bar", new SearchOptions());
            Assert.Matches(regex, "foo.bar");
            Assert.DoesNotMatch(regex, "fooXbar");
        }

        [Fact]
        public void BuildRegex_Regex_UsesPatternDirectly()
        {
            var regex = SearchService.BuildRegex("foo.bar", new SearchOptions(UseRegex: true));
            Assert.Matches(regex, "foo.bar");
            Assert.Matches(regex, "fooXbar");
        }

        [Fact]
        public void ApplyPreserveCase_AllUpper_ReturnsUpper()
        {
            Assert.Equal("GOODBYE", SearchService.ApplyPreserveCase("HELLO", "goodbye"));
        }

        [Fact]
        public void ApplyPreserveCase_AllLower_ReturnsLower()
        {
            Assert.Equal("goodbye", SearchService.ApplyPreserveCase("hello", "GOODBYE"));
        }

        [Fact]
        public void ApplyPreserveCase_TitleCase_ReturnsTitleCase()
        {
            Assert.Equal("Goodbye", SearchService.ApplyPreserveCase("Hello", "goodbye"));
        }

        [Fact]
        public void ApplyPreserveCase_MixedCase_ReturnsAsIs()
        {
            Assert.Equal("goodbye", SearchService.ApplyPreserveCase("hElLo", "goodbye"));
        }
    }
}
