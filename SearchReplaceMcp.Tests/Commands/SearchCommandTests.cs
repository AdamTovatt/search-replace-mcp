using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Tests.Commands
{
    public class SearchCommandTests
    {
        private readonly Mock<ISessionManager> _sessionManagerMock;
        private readonly Mock<ISearchService> _searchServiceMock;

        public SearchCommandTests()
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

            SearchCommand command = new SearchCommand(_sessionManagerMock.Object, _searchServiceMock.Object, "test", new SearchOptions());
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

            SearchCommand command = new SearchCommand(_sessionManagerMock.Object, _searchServiceMock.Object, "test", new SearchOptions());
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No documents in scope", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithMatches_ReturnsSuccess()
        {
            Session session = Session.Create(@"C:\test");
            session.AddedDocuments.Add("file1.cs");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            List<SearchMatch> matches = new List<SearchMatch>
            {
                new SearchMatch("file1.cs", 5, "    var test = true;", 8, 4, "test")
            };
            _searchServiceMock
                .Setup(s => s.SearchAsync(
                    It.IsAny<List<string>>(), It.IsAny<string>(), "test",
                    It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(matches);

            SearchCommand command = new SearchCommand(_sessionManagerMock.Object, _searchServiceMock.Object, "test", new SearchOptions());
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Found 1 match(es)", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoMatches_ReturnsSuccessNoMatches()
        {
            Session session = Session.Create(@"C:\test");
            session.AddedDocuments.Add("file1.cs");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _searchServiceMock
                .Setup(s => s.SearchAsync(
                    It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchMatch>());

            SearchCommand command = new SearchCommand(_sessionManagerMock.Object, _searchServiceMock.Object, "notfound", new SearchOptions());
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("No matches found", result.Message);
        }
    }
}
