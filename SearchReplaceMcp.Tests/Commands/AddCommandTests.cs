using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Tests.Commands
{
    public class AddCommandTests
    {
        private readonly Mock<ISessionManager> _sessionManagerMock;
        private readonly Mock<IDocumentResolver> _documentResolverMock;

        public AddCommandTests()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
            _documentResolverMock = new Mock<IDocumentResolver>();
        }

        [Fact]
        public async Task ExecuteAsync_WithNoSession_ReturnsFailure()
        {
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            AddCommand command = new AddCommand(_sessionManagerMock.Object, _documentResolverMock.Object, new List<string> { "*.cs" });
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No session initialized", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithMatchingFiles_ReturnsSuccess()
        {
            Session session = Session.Create(@"C:\test");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            List<string> resolvedFiles = new List<string> { "file1.cs", "file2.cs" };
            _documentResolverMock
                .Setup(d => d.ResolveAsync(It.IsAny<List<string>>(), session.BasePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(resolvedFiles);

            // Return updated session with documents
            Session updatedSession = Session.Create(@"C:\test");
            updatedSession.AddedDocuments.AddRange(resolvedFiles);
            _sessionManagerMock
                .SetupSequence(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session)
                .ReturnsAsync(updatedSession);

            AddCommand command = new AddCommand(_sessionManagerMock.Object, _documentResolverMock.Object, new List<string> { "**/*.cs" });
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Added 2 file(s)", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoMatchingFiles_ReturnsFailure()
        {
            Session session = Session.Create(@"C:\test");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _documentResolverMock
                .Setup(d => d.ResolveAsync(It.IsAny<List<string>>(), session.BasePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

            AddCommand command = new AddCommand(_sessionManagerMock.Object, _documentResolverMock.Object, new List<string> { "*.xyz" });
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No files matched", result.Message);
        }
    }
}
