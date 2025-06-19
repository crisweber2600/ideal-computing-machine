namespace MetricsPipeline.Core;

public interface IDriveScanner
{
    /// <summary>
    /// Enumerates all directories under the provided root path.
    /// </summary>
    /// <param name="rootPath">Root directory path.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    Task<IEnumerable<string>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates file and directory counts for the specified path.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default);
}
