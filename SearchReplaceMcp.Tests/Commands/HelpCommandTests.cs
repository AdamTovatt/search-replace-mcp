using SearchReplaceMcp.Core.Commands;
using SearchReplaceMcp.Core.Interfaces;

namespace SearchReplaceMcp.Tests.Commands
{
    public class HelpCommandTests
    {
        [Fact]
        public async Task ExecuteAsync_ReturnsSuccessWithHelpText()
        {
            HelpCommand command = new HelpCommand();
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("sr init", result.Message);
            Assert.Contains("sr add", result.Message);
            Assert.Contains("sr search", result.Message);
            Assert.Contains("sr replace", result.Message);
            Assert.Contains("sr preview", result.Message);
            Assert.Contains("sr commit", result.Message);
            Assert.Contains("sr cancel", result.Message);
        }
    }
}
