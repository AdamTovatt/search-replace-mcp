using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using SearchReplaceMcp.Core.Services;

namespace SearchReplaceMcp.Tests.Integration
{
    public class FullWorkflowTests : IDisposable
    {
        private readonly string _testDir;
        private readonly string _originalSessionPath;
        private readonly ISessionStorage _sessionStorage;
        private readonly ISessionManager _sessionManager;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDocumentResolver _documentResolver;
        private readonly ISearchService _searchService;

        public FullWorkflowTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "sr-integration-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDir);

            // Store original session file path for cleanup
            _originalSessionPath = Path.Combine(Path.GetTempPath(), "sr-session.json");

            // Delete any existing session
            if (File.Exists(_originalSessionPath))
            {
                File.Delete(_originalSessionPath);
            }

            _sessionStorage = new SessionStorage();
            _sessionManager = new SessionManager(_sessionStorage);
            _fileOperationService = new FileOperationService();
            _documentResolver = new DocumentResolver(_fileOperationService);
            _searchService = new SearchService(_fileOperationService);
        }

        [Fact]
        public async Task FullWorkflow_InitAddSearchReplacePreviewCommit()
        {
            // Create test files
            File.WriteAllText(Path.Combine(_testDir, "file1.txt"), "hello world\nhello there\ngoodbye\n");
            File.WriteAllText(Path.Combine(_testDir, "file2.txt"), "hello again\nno match here\n");

            // 1. Init
            InitCommand initCommand = new InitCommand(_sessionManager, _testDir);
            CommandResult initResult = await initCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(initResult.Success);

            // 2. Add
            AddCommand addCommand = new AddCommand(_sessionManager, _documentResolver, new List<string> { "**/*.txt" });
            CommandResult addResult = await addCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(addResult.Success);
            Assert.Contains("2 file(s)", addResult.Message);

            // 3. Search
            SearchCommand searchCommand = new SearchCommand(_sessionManager, _searchService, "hello", new SearchOptions());
            CommandResult searchResult = await searchCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(searchResult.Success);
            Assert.Contains("3 match(es)", searchResult.Message);

            // 4. Replace
            ReplaceCommand replaceCommand = new ReplaceCommand(
                _sessionManager, _searchService, "hello", "hi", new SearchOptions());
            CommandResult replaceResult = await replaceCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(replaceResult.Success);
            Assert.Contains("Staged 3 replacement(s)", replaceResult.Message);

            // 5. Preview
            PreviewCommand previewCommand = new PreviewCommand(_sessionManager);
            CommandResult previewResult = await previewCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(previewResult.Success);
            Assert.Contains("3 staged replacement(s)", previewResult.Message);
            Assert.NotNull(previewResult.Details);

            // 6. Commit
            CommitCommand commitCommand = new CommitCommand(_sessionManager, _fileOperationService);
            CommandResult commitResult = await commitCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(commitResult.Success);
            Assert.Contains("Committed 3 replacement(s)", commitResult.Message);

            // Verify file contents
            string file1Content = File.ReadAllText(Path.Combine(_testDir, "file1.txt"));
            string file2Content = File.ReadAllText(Path.Combine(_testDir, "file2.txt"));
            Assert.Contains("hi world", file1Content);
            Assert.Contains("hi there", file1Content);
            Assert.Contains("hi again", file2Content);
            Assert.DoesNotContain("hello", file1Content);
            Assert.DoesNotContain("hello", file2Content);
        }

        [Fact]
        public async Task FullWorkflow_CancelDiscardsStagedReplacements()
        {
            File.WriteAllText(Path.Combine(_testDir, "test.txt"), "hello world\n");

            // Init
            InitCommand initCommand = new InitCommand(_sessionManager, _testDir);
            await initCommand.ExecuteAsync(CancellationToken.None);

            // Add
            AddCommand addCommand = new AddCommand(_sessionManager, _documentResolver, new List<string> { "test.txt" });
            await addCommand.ExecuteAsync(CancellationToken.None);

            // Replace
            ReplaceCommand replaceCommand = new ReplaceCommand(
                _sessionManager, _searchService, "hello", "goodbye", new SearchOptions());
            await replaceCommand.ExecuteAsync(CancellationToken.None);

            // Cancel
            CancelCommand cancelCommand = new CancelCommand(_sessionManager);
            CommandResult cancelResult = await cancelCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(cancelResult.Success);

            // Verify file is unchanged
            string content = File.ReadAllText(Path.Combine(_testDir, "test.txt"));
            Assert.Contains("hello world", content);

            // Verify session is gone
            bool hasSession = await _sessionManager.HasActiveSessionAsync(CancellationToken.None);
            Assert.False(hasSession);
        }

        [Fact]
        public async Task FullWorkflow_CaseSensitiveReplace()
        {
            File.WriteAllText(Path.Combine(_testDir, "case.txt"), "Hello hello HELLO\n");

            // Init, Add
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "case.txt" }).ExecuteAsync(CancellationToken.None);

            // Case-sensitive replace - only match lowercase
            ReplaceCommand replaceCommand = new ReplaceCommand(
                _sessionManager, _searchService, "hello", "world",
                new SearchOptions(MatchCase: true));
            CommandResult result = await replaceCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(result.Success);
            Assert.Contains("Staged 1 replacement(s)", result.Message);

            // Commit
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            string content = File.ReadAllText(Path.Combine(_testDir, "case.txt"));
            Assert.Contains("Hello world HELLO", content);
        }

        [Fact]
        public async Task FullWorkflow_PreserveCaseReplace()
        {
            File.WriteAllText(Path.Combine(_testDir, "preserve.txt"), "Hello\nhello\nHELLO\n");

            // Init, Add
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "preserve.txt" }).ExecuteAsync(CancellationToken.None);

            // Preserve-case replace
            ReplaceCommand replaceCommand = new ReplaceCommand(
                _sessionManager, _searchService, "hello", "goodbye",
                new SearchOptions(PreserveCase: true));
            CommandResult result = await replaceCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(result.Success);

            // Commit
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            string[] lines = File.ReadAllLines(Path.Combine(_testDir, "preserve.txt"));
            Assert.Equal("Goodbye", lines[0]);
            Assert.Equal("goodbye", lines[1]);
            Assert.Equal("GOODBYE", lines[2]);
        }

        [Fact]
        public async Task FullWorkflow_WholeWordReplace()
        {
            File.WriteAllText(Path.Combine(_testDir, "word.txt"), "test testing tester\na test here\n");

            // Init, Add
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "word.txt" }).ExecuteAsync(CancellationToken.None);

            // Whole word replace
            ReplaceCommand replaceCommand = new ReplaceCommand(
                _sessionManager, _searchService, "test", "check",
                new SearchOptions(MatchWholeWord: true));
            CommandResult result = await replaceCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(result.Success);
            Assert.Contains("Staged 2 replacement(s)", result.Message);

            // Commit
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            string[] lines = File.ReadAllLines(Path.Combine(_testDir, "word.txt"));
            Assert.Equal("check testing tester", lines[0]);
            Assert.Equal("a check here", lines[1]);
        }

        [Fact]
        public async Task FullWorkflow_RegexReplace()
        {
            File.WriteAllText(Path.Combine(_testDir, "regex.txt"), "foo123bar\nfoo456bar\nno match\n");

            // Init, Add
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "regex.txt" }).ExecuteAsync(CancellationToken.None);

            // Regex replace
            ReplaceCommand replaceCommand = new ReplaceCommand(
                _sessionManager, _searchService, @"foo(\d+)bar", "baz$1qux",
                new SearchOptions(UseRegex: true));
            CommandResult result = await replaceCommand.ExecuteAsync(CancellationToken.None);
            Assert.True(result.Success);

            // Commit
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            string[] lines = File.ReadAllLines(Path.Combine(_testDir, "regex.txt"));
            Assert.Equal("baz123qux", lines[0]);
            Assert.Equal("baz456qux", lines[1]);
            Assert.Equal("no match", lines[2]);
        }

        [Fact]
        public async Task FullWorkflow_DuplicateReplaceCallsDoNotCorruptOutput()
        {
            File.WriteAllText(Path.Combine(_testDir, "dup.txt"), "hello world\n");

            // Init, Add
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "dup.txt" }).ExecuteAsync(CancellationToken.None);

            // Call replace twice with the same pattern
            await new ReplaceCommand(_sessionManager, _searchService, "hello", "goodbye", new SearchOptions())
                .ExecuteAsync(CancellationToken.None);
            await new ReplaceCommand(_sessionManager, _searchService, "hello", "goodbye", new SearchOptions())
                .ExecuteAsync(CancellationToken.None);

            // Commit
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            // Should be "goodbye world", not "goodbyeye world" or other corruption
            string[] lines = File.ReadAllLines(Path.Combine(_testDir, "dup.txt"));
            Assert.Equal("goodbye world", lines[0]);
        }

        [Fact]
        public async Task FullWorkflow_MultipleNonOverlappingReplaceCalls_WorkCorrectly()
        {
            File.WriteAllText(Path.Combine(_testDir, "multi.txt"), "hello world\n");

            // Init, Add
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "multi.txt" }).ExecuteAsync(CancellationToken.None);

            // Two separate replace calls for different patterns
            await new ReplaceCommand(_sessionManager, _searchService, "hello", "goodbye", new SearchOptions())
                .ExecuteAsync(CancellationToken.None);
            await new ReplaceCommand(_sessionManager, _searchService, "world", "earth", new SearchOptions())
                .ExecuteAsync(CancellationToken.None);

            // Commit
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            string[] lines = File.ReadAllLines(Path.Combine(_testDir, "multi.txt"));
            Assert.Equal("goodbye earth", lines[0]);
        }

        [Fact]
        public async Task FullWorkflow_StaleFileIsSkippedOnCommit()
        {
            string filePath = Path.Combine(_testDir, "stale.txt");
            File.WriteAllText(filePath, "hello world\n");

            // Init, Add, Replace
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "stale.txt" }).ExecuteAsync(CancellationToken.None);
            await new ReplaceCommand(_sessionManager, _searchService, "hello", "goodbye", new SearchOptions())
                .ExecuteAsync(CancellationToken.None);

            // Externally modify the file between replace and commit
            File.WriteAllText(filePath, "hello world modified\n");

            // Commit - should skip the stale file
            CommandResult result = await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);
            Assert.True(result.Success);
            Assert.Contains("Skipped 1 file(s)", result.Message);

            // File should be unchanged (the external modification should remain)
            string content = File.ReadAllText(filePath);
            Assert.Contains("hello world modified", content);
            Assert.DoesNotContain("goodbye", content);
        }

        [Fact]
        public async Task FullWorkflow_PreservesUnixLineEndings()
        {
            // Write file with explicit Unix line endings
            string filePath = Path.Combine(_testDir, "unix.txt");
            File.WriteAllText(filePath, "hello world\nsecond line\n");

            // Init, Add, Replace, Commit
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "unix.txt" }).ExecuteAsync(CancellationToken.None);
            await new ReplaceCommand(_sessionManager, _searchService, "hello", "goodbye", new SearchOptions())
                .ExecuteAsync(CancellationToken.None);
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            // Verify Unix line endings are preserved
            string content = File.ReadAllText(filePath);
            Assert.Contains("goodbye world", content);
            Assert.DoesNotContain("\r\n", content);
            Assert.Contains("\n", content);
        }

        [Fact]
        public async Task FullWorkflow_PreservesWindowsLineEndings()
        {
            // Write file with explicit Windows line endings
            string filePath = Path.Combine(_testDir, "windows.txt");
            File.WriteAllText(filePath, "hello world\r\nsecond line\r\n");

            // Init, Add, Replace, Commit
            await new InitCommand(_sessionManager, _testDir).ExecuteAsync(CancellationToken.None);
            await new AddCommand(_sessionManager, _documentResolver, new List<string> { "windows.txt" }).ExecuteAsync(CancellationToken.None);
            await new ReplaceCommand(_sessionManager, _searchService, "hello", "goodbye", new SearchOptions())
                .ExecuteAsync(CancellationToken.None);
            await new CommitCommand(_sessionManager, _fileOperationService).ExecuteAsync(CancellationToken.None);

            // Verify Windows line endings are preserved
            string content = File.ReadAllText(filePath);
            Assert.Contains("goodbye world", content);
            Assert.Contains("\r\n", content);
        }

        public void Dispose()
        {
            // Clean up test files
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }

            // Clean up session file
            if (File.Exists(_originalSessionPath))
            {
                File.Delete(_originalSessionPath);
            }
        }
    }
}
