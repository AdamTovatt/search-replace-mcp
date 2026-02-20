using Moq;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using SearchReplaceMcp.Core.Services;

namespace SearchReplaceMcp.Tests.Services
{
    public class SessionManagerTests
    {
        private readonly Mock<ISessionStorage> _sessionStorageMock;
        private readonly SessionManager _sessionManager;

        public SessionManagerTests()
        {
            _sessionStorageMock = new Mock<ISessionStorage>();
            _sessionManager = new SessionManager(_sessionStorageMock.Object);
        }

        [Fact]
        public async Task GetActiveSessionAsync_ReturnsSessionFromStorage()
        {
            Session session = Session.Create(@"C:\test");
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            Session? result = await _sessionManager.GetActiveSessionAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(session.SessionId, result.SessionId);
        }

        [Fact]
        public async Task GetActiveSessionAsync_NoSession_ReturnsNull()
        {
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            Session? result = await _sessionManager.GetActiveSessionAsync(CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateSessionAsync_WhenSessionExists_ThrowsInvalidOperationException()
        {
            Session existing = Session.Create(@"C:\existing");
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.CreateSessionAsync(@"C:\test", CancellationToken.None));
        }

        [Fact]
        public async Task AddDocumentsAsync_WithNoSession_ThrowsInvalidOperationException()
        {
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.AddDocumentsAsync(new List<string> { "file.cs" }, CancellationToken.None));
        }

        [Fact]
        public async Task AddDocumentsAsync_DeduplicatesDocuments()
        {
            Session session = Session.Create(@"C:\test");
            session.AddedDocuments.Add("file1.cs");
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            await _sessionManager.AddDocumentsAsync(new List<string> { "file1.cs", "file2.cs" }, CancellationToken.None);

            _sessionStorageMock.Verify(
                s => s.SaveSessionAsync(
                    It.Is<Session>(sess => sess.AddedDocuments.Count == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddReplacementsAsync_WithNoSession_ThrowsInvalidOperationException()
        {
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.AddReplacementsAsync(new List<StagedReplacement>(), CancellationToken.None));
        }

        [Fact]
        public async Task AddReplacementsAsync_AppendsToExisting()
        {
            Session session = Session.Create(@"C:\test");
            session.StagedReplacements.Add(new StagedReplacement("file1.cs", 1, "old", 0, 3, "new"));
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            List<StagedReplacement> newReplacements = new List<StagedReplacement>
            {
                new StagedReplacement("file2.cs", 2, "old2", 0, 4, "new2")
            };

            await _sessionManager.AddReplacementsAsync(newReplacements, CancellationToken.None);

            _sessionStorageMock.Verify(
                s => s.SaveSessionAsync(
                    It.Is<Session>(sess => sess.StagedReplacements.Count == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ClearReplacementsAsync_ClearsAll()
        {
            Session session = Session.Create(@"C:\test");
            session.StagedReplacements.Add(new StagedReplacement("file1.cs", 1, "old", 0, 3, "new"));
            _sessionStorageMock
                .Setup(s => s.LoadSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            await _sessionManager.ClearReplacementsAsync(CancellationToken.None);

            _sessionStorageMock.Verify(
                s => s.SaveSessionAsync(
                    It.Is<Session>(sess => sess.StagedReplacements.Count == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CancelSessionAsync_WithNoSession_ThrowsInvalidOperationException()
        {
            _sessionStorageMock
                .Setup(s => s.SessionExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.CancelSessionAsync(CancellationToken.None));
        }

        [Fact]
        public async Task CancelSessionAsync_DeletesSession()
        {
            _sessionStorageMock
                .Setup(s => s.SessionExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await _sessionManager.CancelSessionAsync(CancellationToken.None);

            _sessionStorageMock.Verify(s => s.DeleteSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HasActiveSessionAsync_ReturnsStorageResult()
        {
            _sessionStorageMock
                .Setup(s => s.SessionExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            bool result = await _sessionManager.HasActiveSessionAsync(CancellationToken.None);

            Assert.True(result);
        }
    }
}
