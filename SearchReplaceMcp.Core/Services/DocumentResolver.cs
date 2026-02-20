using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SearchReplaceMcp.Core.Interfaces;

namespace SearchReplaceMcp.Core.Services
{
    /// <summary>
    /// Resolves file paths from globs, directories, and individual file paths.
    /// </summary>
    public class DocumentResolver : IDocumentResolver
    {
        private readonly IFileOperationService _fileOperationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentResolver"/> class.
        /// </summary>
        /// <param name="fileOperationService">The file operation service for binary detection.</param>
        public DocumentResolver(IFileOperationService fileOperationService)
        {
            _fileOperationService = fileOperationService;
        }

        /// <inheritdoc/>
        public async Task<List<string>> ResolveAsync(List<string> paths, string basePath, CancellationToken cancellationToken)
        {
            HashSet<string> resolvedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in paths)
            {
                List<string> expanded = ExpandPath(path, basePath);

                foreach (string filePath in expanded)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Skip binary files
                    bool isBinary = await _fileOperationService.IsBinaryFileAsync(filePath, basePath, cancellationToken);
                    if (!isBinary)
                    {
                        resolvedPaths.Add(filePath);
                    }
                }
            }

            return resolvedPaths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> ExpandPath(string path, string basePath)
        {
            List<string> results = new List<string>();

            // Check if this is a glob pattern
            if (IsGlobPattern(path))
            {
                Matcher matcher = new Matcher();
                matcher.AddInclude(path);
                PatternMatchingResult matchResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(basePath)));

                foreach (FilePatternMatch match in matchResult.Files)
                {
                    string normalizedPath = NormalizePath(match.Path);
                    results.Add(normalizedPath);
                }
            }
            else
            {
                string fullPath = Path.Combine(basePath, path.Replace('/', Path.DirectorySeparatorChar));

                if (Directory.Exists(fullPath))
                {
                    // Recursively enumerate directory
                    foreach (string file in Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories))
                    {
                        string relativePath = Path.GetRelativePath(basePath, file);
                        string normalizedPath = NormalizePath(relativePath);
                        results.Add(normalizedPath);
                    }
                }
                else if (File.Exists(fullPath))
                {
                    string relativePath = Path.GetRelativePath(basePath, fullPath);
                    string normalizedPath = NormalizePath(relativePath);
                    results.Add(normalizedPath);
                }
            }

            return results;
        }

        private static bool IsGlobPattern(string path)
        {
            return path.Contains('*') || path.Contains('?') || path.Contains('[');
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
