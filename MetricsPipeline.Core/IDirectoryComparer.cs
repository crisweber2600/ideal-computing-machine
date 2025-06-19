namespace MetricsPipeline.Core;

/// <summary>
/// Compares two directories and identifies differences.
/// </summary>
public interface IDirectoryComparer
{
    /// <summary>
    /// Compares the source directory with the destination directory.
    /// </summary>
    /// <param name="sourcePath">Path to the source directory.</param>
    /// <param name="destinationPath">Path to the destination directory.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    Task<IReadOnlyCollection<MismatchRow>> CompareAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
