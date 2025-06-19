using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsPipeline.Core;

/// <summary>
/// Streams comparison results as CSV to a destination stream.
/// </summary>
public sealed class CsvExporter
{
    /// <summary>
    /// Writes the sequence of <see cref="CountsDifference"/> to the provided stream.
    /// </summary>
    /// <param name="differences">Differences to export.</param>
    /// <param name="stream">Destination stream; e.g. a FileStream or Console.Out.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    public async Task ExportAsync(
        IEnumerable<CountsDifference> differences,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteLineAsync("Path,LeftFiles,LeftDirs,LeftBytes,RightFiles,RightDirs,RightBytes").ConfigureAwait(false);
        foreach (var diff in differences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = string.Join(',',
                diff.Path,
                diff.Left?.FileCount.ToString() ?? string.Empty,
                diff.Left?.DirectoryCount.ToString() ?? string.Empty,
                diff.Left?.TotalBytes.ToString() ?? string.Empty,
                diff.Right?.FileCount.ToString() ?? string.Empty,
                diff.Right?.DirectoryCount.ToString() ?? string.Empty,
                diff.Right?.TotalBytes.ToString() ?? string.Empty);
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }
        await writer.FlushAsync().ConfigureAwait(false);
    }
}
