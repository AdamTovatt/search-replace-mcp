using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Tests.Commands
{
    public class ReplaceCommandTests
    {
        private readonly Mock<ISessionManager> _sessionManagerMock;
        private readonly Mock<ISearchService> _searchServiceMock;

        public ReplaceCommandTests()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
            _searchServiceMock = new Mock<ISearchService>();
        }

        [Fact]
        public async Task ExecuteAsync_WithNoSession_ReturnsFailure()
        {
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            ReplaceCommand command = new ReplaceCommand(
                _sessionManagerMock.Object, _searchServiceMock.Object, "old", "new", new SearchOptions());
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No session initialized", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoDocuments_ReturnsFailure()
        {
            Session session = Session.Create(@"C:\test");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            ReplaceCommand command = new ReplaceCommand(
                _sessionManagerMock.Object, _searchServiceMock.Object, "old", "new", new SearchOptions());
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No documents in scope", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithMatches_StagesReplacementsAndReturnsSuccess()
        {
            Session session = Session.Create(@"C:\test");
            session.AddedDocuments.Add("file1.cs");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            List<StagedReplacement> replacements = new List<StagedReplacement>
            {
                new StagedReplacement("file1.cs", 5, "    var old = true;", 8, 3, "new")
            };
            _searchServiceMock
                .Setup(s => s.SearchAndStageReplacementsAsync(
                    It.IsAny<List<string>>(), It.IsAny<string>(), "old", "new",
                    It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(replacements);

            ReplaceCommand command = new ReplaceCommand(
                _sessionManagerMock.Object, _searchServiceMock.Object, "old", "new", new SearchOptions());
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Staged 1 replacement(s)", result.Message);
            _sessionManagerMock.Verify(s => s.AddReplacementsAsync(replacements, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoMatches_ReturnsSuccessNothingStaged()
        {
            Session session = Session.Create(@"C:\test");
            session.AddedDocuments.Add("file1.cs");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _searchServiceMock
                .Setup(s => s.SearchAndStageReplacementsAsync(
                    It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StagedReplacement>());

            ReplaceCommand command = new ReplaceCommand(
                _sessionManagerMock.Object, _searchServiceMock.Object, "notfound", "new", new SearchOptions());
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("No matches found", result.Message);
        }
    }
}
