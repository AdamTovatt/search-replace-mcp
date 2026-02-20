using Moq;
using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Services;

namespace SearchReplaceMcp.Tests.Services
{
    public class CommandFactoryTests
    {
        private readonly CommandFactory _factory;

        public CommandFactoryTests()
        {
            var sessionManagerMock = new Mock<ISessionManager>();
            var searchServiceMock = new Mock<ISearchService>();
            var fileOperationServiceMock = new Mock<IFileOperationService>();
            var documentResolverMock = new Mock<IDocumentResolver>();

            _factory = new CommandFactory(
                sessionManagerMock.Object,
                searchServiceMock.Object,
                fileOperationServiceMock.Object,
                documentResolverMock.Object);
        }

        [Fact]
        public void CreateCommand_NoArgs_ReturnsHelpCommand()
        {
            ICommand command = _factory.CreateCommand(Array.Empty<string>());
            Assert.IsType<HelpCommand>(command);
        }

        [Theory]
        [InlineData("help")]
        [InlineData("--help")]
        [InlineData("-h")]
        public void CreateCommand_HelpArgs_ReturnsHelpCommand(string arg)
        {
            ICommand command = _factory.CreateCommand(new[] { arg });
            Assert.IsType<HelpCommand>(command);
        }

        [Fact]
        public void CreateCommand_Init_ReturnsInitCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "init" });
            Assert.IsType<InitCommand>(command);
        }

        [Fact]
        public void CreateCommand_InitWithPath_ReturnsInitCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "init", @"C:\test" });
            Assert.IsType<InitCommand>(command);
        }

        [Fact]
        public void CreateCommand_Add_ReturnsAddCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "add", "**/*.cs" });
            Assert.IsType<AddCommand>(command);
        }

        [Fact]
        public void CreateCommand_AddMultiplePaths_ReturnsAddCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "add", "**/*.cs", "**/*.txt" });
            Assert.IsType<AddCommand>(command);
        }

        [Fact]
        public void CreateCommand_AddWithNoPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _factory.CreateCommand(new[] { "add" }));
        }

        [Fact]
        public void CreateCommand_Search_ReturnsSearchCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "search", "hello" });
            Assert.IsType<SearchCommand>(command);
        }

        [Fact]
        public void CreateCommand_SearchWithOptions_ReturnsSearchCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "search", "hello", "-c", "-w", "-r" });
            Assert.IsType<SearchCommand>(command);
        }

        [Fact]
        public void CreateCommand_SearchWithNoPattern_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _factory.CreateCommand(new[] { "search" }));
        }

        [Fact]
        public void CreateCommand_Replace_ReturnsReplaceCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "replace", "old", "new" });
            Assert.IsType<ReplaceCommand>(command);
        }

        [Fact]
        public void CreateCommand_ReplaceWithOptions_ReturnsReplaceCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "replace", "old", "new", "-c", "-w", "-p" });
            Assert.IsType<ReplaceCommand>(command);
        }

        [Fact]
        public void CreateCommand_ReplaceWithNoReplacement_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _factory.CreateCommand(new[] { "replace", "old" }));
        }

        [Fact]
        public void CreateCommand_Preview_ReturnsPreviewCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "preview" });
            Assert.IsType<PreviewCommand>(command);
        }

        [Fact]
        public void CreateCommand_Commit_ReturnsCommitCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "commit" });
            Assert.IsType<CommitCommand>(command);
        }

        [Fact]
        public void CreateCommand_Cancel_ReturnsCancelCommand()
        {
            ICommand command = _factory.CreateCommand(new[] { "cancel" });
            Assert.IsType<CancelCommand>(command);
        }

        [Fact]
        public void CreateCommand_UnknownCommand_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _factory.CreateCommand(new[] { "unknown" }));
        }
    }
}
