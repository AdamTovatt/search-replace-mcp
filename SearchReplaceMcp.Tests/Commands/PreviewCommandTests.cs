using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;

namespace SearchReplaceMcp.Tests.Commands
{
    public class PreviewCommandTests
    {
        private readonly Mock<ISessionManager> _sessionManagerMock;

        public PreviewCommandTests()
        {
            _sessionManagerMock = new Mock<ISessionManager>();
        }

        [Fact]
        public async Task ExecuteAsync_WithNoSession_ReturnsFailure()
        {
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            PreviewCommand command = new PreviewCommand(_sessionManagerMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No session initialized", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoReplacements_ReturnsSuccessNoReplacements()
        {
            Session session = Session.Create(@"C:\test");
            session.AddedDocuments.Add("file1.cs");
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            PreviewCommand command = new PreviewCommand(_sessionManagerMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("No staged replacements", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithReplacements_ReturnsFormattedPreview()
        {
            Session session = Session.Create(@"C:\test");
            session.AddedDocuments.Add("file1.cs");
            session.StagedReplacements.Add(new StagedReplacement("file1.cs", 5, "    var old = true;", 8, 3, "new"));
            _sessionManagerMock
                .Setup(s => s.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            PreviewCommand command = new PreviewCommand(_sessionManagerMock.Object);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("1 staged replacement(s)", result.Message);
            Assert.NotNull(result.Details);
            Assert.Contains("var old = true;", result.Details);
            Assert.Contains("var new = true;", result.Details);
        }

        [Fact]
        public void ApplyReplacementsToLine_SingleReplacement_WorksCorrectly()
        {
            List<StagedReplacement> replacements = new List<StagedReplacement>
            {
                new StagedReplacement("file.cs", 1, "hello world", 0, 5, "goodbye")
            };

            string result = PreviewCommand.ApplyReplacementsToLine("hello world", replacements);

            Assert.Equal("goodbye world", result);
        }

        [Fact]
        public void ApplyReplacementsToLine_MultipleReplacements_AppliesRightToLeft()
        {
            List<StagedReplacement> replacements = new List<StagedReplacement>
            {
                new StagedReplacement("file.cs", 1, "hello world hello", 0, 5, "hi"),
                new StagedReplacement("file.cs", 1, "hello world hello", 12, 5, "hi")
            };

            string result = PreviewCommand.ApplyReplacementsToLine("hello world hello", replacements);

            Assert.Equal("hi world hi", result);
        }
    }
}
