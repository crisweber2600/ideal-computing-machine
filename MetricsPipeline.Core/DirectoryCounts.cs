namespace MetricsPipeline.Core;

/// <summary>
/// Represents counts and sizes for a directory scan.
/// </summary>
/// <param name="FileCount">Number of files discovered.</param>
/// <param name="DirectoryCount">Number of directories discovered.</param>
/// <param name="TotalBytes">Total bytes for all files.</param>
public record DirectoryCounts(int FileCount, int DirectoryCount, long TotalBytes);
