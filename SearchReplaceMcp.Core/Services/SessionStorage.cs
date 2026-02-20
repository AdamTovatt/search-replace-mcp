using SearchReplaceMcp.Core.Interfaces;
using SearchReplaceMcp.Core.Models;
using System.Text.Json;

namespace SearchReplaceMcp.Core.Services
{
    /// <summary>
    /// Handles persistence of session data in the system temp directory.
    /// </summary>
    public class SessionStorage : ISessionStorage
    {
        private const string SessionFileName = "sr-session.json";
        private readonly string _sessionFilePath;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStorage"/> class.
        /// </summary>
        public SessionStorage()
        {
            string tempPath = Path.GetTempPath();
            _sessionFilePath = Path.Combine(tempPath, SessionFileName);
        }

        /// <inheritdoc/>
        public async Task<Session?> LoadSessionAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_sessionFilePath))
            {
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(_sessionFilePath, cancellationToken);
                Session? session = JsonSerializer.Deserialize<Session>(json, JsonOptions);
                return session;
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                // If the session file is corrupted or unreadable, delete it
                await DeleteSessionAsync(cancellationToken);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SaveSessionAsync(Session session, CancellationToken cancellationToken)
        {
            string json = JsonSerializer.Serialize(session, JsonOptions);
            await File.WriteAllTextAsync(_sessionFilePath, json, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DeleteSessionAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> SessionExistsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(File.Exists(_sessionFilePath));
        }
    }
}
