using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Tests.Commands
{
    public class CommitCommandTests
    {
        private readonly Mock<ISessionManager> _sessionManagerMock;
        private readonly Mock<IFileOperationService> _fileOperationServiceMock;

        public CommitCommandTests()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
            _fileOperationServiceMock = new Mock<IFileOperationService>();
        }

        [Fact]
        public async Task ExecuteAsync_WithNoSession_ReturnsFailure()
        {
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            CommitCommand command = new CommitCommand(_sessionManagerMock.Object, _fileOperationServiceMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No session initialized", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoReplacements_ReturnsFailure()
        {
            Session session = Session.Create(@"C:\test");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            CommitCommand command = new CommitCommand(_sessionManagerMock.Object, _fileOperationServiceMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No staged replacements to commit", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithReplacements_AppliesAndClearsSession()
        {
            Session session = Session.Create(@"C:\test");
            session.StagedReplacements.Add(new StagedReplacement("file1.cs", 1, "hello world", 0, 5, "goodbye"));
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file1.cs", session.BasePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "hello world", "second line" });

            CommitCommand command = new CommitCommand(_sessionManagerMock.Object, _fileOperationServiceMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Committed 1 replacement(s)", result.Message);

            _fileOperationServiceMock.Verify(
                f => f.WriteFileLinesAsync("file1.cs", session.BasePath,
                    It.Is<string[]>(lines => lines[0] == "goodbye world"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _sessionManagerMock.Verify(s => s.CancelSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleReplacementsOnSameLine_AppliesRightToLeft()
        {
            Session session = Session.Create(@"C:\test");
            session.StagedReplacements.Add(new StagedReplacement("file1.cs", 1, "aaa bbb aaa", 0, 3, "xxx"));
            session.StagedReplacements.Add(new StagedReplacement("file1.cs", 1, "aaa bbb aaa", 8, 3, "xxx"));
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _fileOperationServiceMock
                .Setup(f => f.ReadFileLinesAsync("file1.cs", session.BasePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "aaa bbb aaa" });

            CommitCommand command = new CommitCommand(_sessionManagerMock.Object, _fileOperationServiceMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);

            _fileOperationServiceMock.Verify(
                f => f.WriteFileLinesAsync("file1.cs", session.BasePath,
                    It.Is<string[]>(lines => lines[0] == "xxx bbb xxx"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
