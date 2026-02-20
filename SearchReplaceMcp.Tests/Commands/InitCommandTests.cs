using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Tests.Commands
{
    public class InitCommandTests
    {
        private readonly Mock<ISessionManager> _sessionManagerMock;

        public InitCommandTests()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
        }

        [Fact]
        public async Task ExecuteAsync_WithValidPath_ReturnsSuccess()
        {
            Session session = Session.Create(@"C:\test\path");
            _sessionManagerMock
                .Setup(s => s.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            InitCommand command = new InitCommand(_sessionManagerMock.Object, @"C:\test\path");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Session initialized at:", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSessionAlreadyExists_ReturnsFailure()
        {
            _sessionManagerMock
                .Setup(s => s.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Session already active"));

            InitCommand command = new InitCommand(_sessionManagerMock.Object, @"C:\test\path");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Error:", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WhenDirectoryNotFound_ReturnsFailure()
        {
            _sessionManagerMock
                .Setup(s => s.CreateSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DirectoryNotFoundException("Directory not found"));

            InitCommand command = new InitCommand(_sessionManagerMock.Object, @"C:\nonexistent");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Error:", result.Message);
        }
    }
}
