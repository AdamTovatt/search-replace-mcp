using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SearchReplaceMcp.Cli
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Check if running in MCP server mode
            bool isMcpMode = args.Length > 0 &&
                (args[0].Equals("--mcp", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("mcp", StringComparison.OrdinalIgnoreCase));

            if (isMcpMode)
            {
                // Run as MCP server
                return await RunMcpServerAsync(args);
            }
            else
            {
                // Run as CLI tool
                return await RunCliAsync(args);
            }
        }

        private static async Task<int> RunCliAsync(string[] args)
        {
            // Set up dependency injection
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            try
            {
                // Create and execute the command
                ICommandFactory commandFactory = serviceProvider.GetRequiredService<ICommandFactory>();
                ICommand command = commandFactory.CreateCommand(args);
                CommandResult result = await command.ExecuteAsync(CancellationToken.None);

                // Display the result
                Console.WriteLine(result.Message);
                if (!string.IsNullOrEmpty(result.Details))
                {
                    Console.WriteLine(result.Details);
                }

                return result.Success ? 0 : 1;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunMcpServerAsync(string[] args)
        {
            try
            {
                HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

                // Configure logging to stderr
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Trace;
                });

                // Register services
                ConfigureServices(builder.Services);

                // Configure MCP server
                builder.Services
                    .AddMcpServer()
                    .WithStdioServerTransport()
                    .WithToolsFromAssembly();

                // Register MCP tools class
                builder.Services.AddSingleton<McpTools>();

                IHost host = builder.Build();
                await host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"MCP Server error: {ex.Message}");
                return 1;
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Register core services
            services.AddSingleton<ISessionStorage, SessionStorage>();
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<IFileOperationService, FileOperationService>();
            services.AddSingleton<ISearchService, SearchService>();
            services.AddSingleton<IDocumentResolver, DocumentResolver>();
            services.AddSingleton<ICommandFactory, CommandFactory>();
        }
    }
}
