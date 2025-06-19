namespace MetricsPipeline.Core;

public record DirectoryEntry(string Id, string Name, string? QuickXorHash = null);

public interface IDriveScanner
{
    /// <summary>
    /// Enumerates all directories under the provided root path.
    /// Returns identifiers that can be used for recursive scanning in addition
    /// to the directory names.
    /// </summary>
    /// <param name="rootPath">Root directory path.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    Task<IEnumerable<DirectoryEntry>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates file and directory counts for the specified path.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default);
}
