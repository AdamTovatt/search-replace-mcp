using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;

namespace SearchReplaceMcp.Tests.Commands
{
    public class CancelCommandTests
    {
        private readonly Mock<ISessionManager> _sessionManagerMock;

        public CancelCommandTests()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
        }

        [Fact]
        public async Task ExecuteAsync_WithActiveSession_ReturnsSuccess()
        {
            _sessionManagerMock
                .Setup(s => s.CancelSessionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            CancelCommand command = new CancelCommand(_sessionManagerMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Session cancelled", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoSession_ReturnsFailure()
        {
            _sessionManagerMock
                .Setup(s => s.CancelSessionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("No session initialized"));

            CancelCommand command = new CancelCommand(_sessionManagerMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Error:", result.Message);
        }
    }
}
