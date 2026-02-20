using Moq;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Services;

namespace SearchReplaceMcp.Tests.Services
{
    public class DocumentResolverTests : IDisposable
    {
        private readonly Mock<IFileOperationService> _fileOperationServiceMock;
        private readonly DocumentResolver _resolver;
        private readonly string _testDir;

        public DocumentResolverTests()
        {
            _fileOperationServiceMock = new Mock<IFileOperationService>();
            _resolver = new DocumentResolver(_fileOperationServiceMock.Object);

            // Create a temp directory for tests
            _testDir = Path.Combine(Path.GetTempPath(), "sr-test-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDir);
        }

        private void CreateTestFile(string relativePath, string content = "test content")
        {
            string fullPath = Path.Combine(_testDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(fullPath, content);
        }

        [Fact]
        public async Task ResolveAsync_SingleFile_ReturnsFile()
        {
            CreateTestFile("test.txt");
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync(It.IsAny<string>(), _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            List<string> result = await _resolver.ResolveAsync(
                new List<string> { "test.txt" }, _testDir, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("test.txt", result[0]);
        }

        [Fact]
        public async Task ResolveAsync_Directory_ReturnsAllFiles()
        {
            CreateTestFile("subdir/file1.txt");
            CreateTestFile("subdir/file2.txt");
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync(It.IsAny<string>(), _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            List<string> result = await _resolver.ResolveAsync(
                new List<string> { "subdir" }, _testDir, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Contains("file1.txt"));
            Assert.Contains(result, p => p.Contains("file2.txt"));
        }

        [Fact]
        public async Task ResolveAsync_GlobPattern_MatchesFiles()
        {
            CreateTestFile("src/app.cs");
            CreateTestFile("src/util.cs");
            CreateTestFile("src/readme.md");
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync(It.IsAny<string>(), _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            List<string> result = await _resolver.ResolveAsync(
                new List<string> { "**/*.cs" }, _testDir, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.EndsWith(".cs", p));
        }

        [Fact]
        public async Task ResolveAsync_SkipsBinaryFiles()
        {
            CreateTestFile("text.txt");
            CreateTestFile("binary.bin");
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync("text.txt", _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync("binary.bin", _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            List<string> result = await _resolver.ResolveAsync(
                new List<string> { "text.txt", "binary.bin" }, _testDir, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("text.txt", result[0]);
        }

        [Fact]
        public async Task ResolveAsync_DeduplicatesPaths()
        {
            CreateTestFile("file.txt");
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync(It.IsAny<string>(), _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            List<string> result = await _resolver.ResolveAsync(
                new List<string> { "file.txt", "file.txt" }, _testDir, CancellationToken.None);

            Assert.Single(result);
        }

        [Fact]
        public async Task ResolveAsync_NormalizesToForwardSlashes()
        {
            CreateTestFile("subdir/file.txt");
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync(It.IsAny<string>(), _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            List<string> result = await _resolver.ResolveAsync(
                new List<string> { "subdir" }, _testDir, CancellationToken.None);

            Assert.Single(result);
            Assert.DoesNotContain("\\", result[0]);
            Assert.Contains("/", result[0]);
        }

        [Fact]
        public async Task ResolveAsync_NonexistentPath_ReturnsEmpty()
        {
            _fileOperationServiceMock
                .Setup(f => f.IsBinaryFileAsync(It.IsAny<string>(), _testDir, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            List<string> result = await _resolver.ResolveAsync(
                new List<string> { "nonexistent.txt" }, _testDir, CancellationToken.None);

            Assert.Empty(result);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
    }
}
