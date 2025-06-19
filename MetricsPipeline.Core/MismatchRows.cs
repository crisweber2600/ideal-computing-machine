namespace MetricsPipeline.Core;

/// <summary>
/// Base record for directory comparison mismatches.
/// </summary>
/// <param name="RelativePath">Path relative to the comparison root.</param>
public abstract record MismatchRow(string RelativePath);

/// <summary>
/// Represents a file or directory missing from one side of the comparison.
/// </summary>
/// <param name="RelativePath">Path relative to the comparison root.</param>
/// <param name="MissingOnSource">True if missing from the source directory.</param>
public record MissingEntryRow(string RelativePath, bool MissingOnSource) : MismatchRow(RelativePath);

/// <summary>
/// Represents a size mismatch for a file present in both directories.
/// </summary>
/// <param name="RelativePath">Path relative to the comparison root.</param>
/// <param name="SourceSize">Size of the source file.</param>
/// <param name="DestinationSize">Size of the destination file.</param>
public record SizeMismatchRow(string RelativePath, long SourceSize, long DestinationSize) : MismatchRow(RelativePath);
