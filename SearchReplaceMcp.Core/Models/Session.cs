using System.Text.Json.Serialization;

namespace SearchReplaceMcp.Core.Models
{
    /// <summary>
    /// Represents a search-and-replace session containing scoped documents and staged replacements.
    /// </summary>
    public class Session
    {
        /// <summary>
        /// Gets or initializes the unique identifier for this session.
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string SessionId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the base path for this session.
        /// </summary>
        [JsonPropertyName("basePath")]
        public string BasePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the list of added document paths (relative, forward slashes).
        /// </summary>
        [JsonPropertyName("addedDocuments")]
        public List<string> AddedDocuments { get; init; } = new List<string>();

        /// <summary>
        /// Gets or initializes the list of staged replacements.
        /// </summary>
        [JsonPropertyName("stagedReplacements")]
        public List<StagedReplacement> StagedReplacements { get; init; } = new List<StagedReplacement>();

        /// <summary>
        /// Gets or initializes the creation timestamp of this session.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new session with the specified base path.
        /// </summary>
        /// <param name="basePath">The base directory path for file operations.</param>
        /// <returns>A new session instance.</returns>
        public static Session Create(string basePath)
        {
            return new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                BasePath = basePath,
                AddedDocuments = new List<string>(),
                StagedReplacements = new List<StagedReplacement>(),
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}
