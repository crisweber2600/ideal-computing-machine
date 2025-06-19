using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsPipeline.Core;

/// <summary>
/// Compares two directories using an <see cref="IDriveScanner"/> implementation.
/// </summary>
public sealed class DirectoryComparer : IDirectoryComparer
{
    private readonly IDriveScanner _scanner;

    public DirectoryComparer(IDriveScanner scanner)
    {
        _scanner = scanner;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<MismatchRow>> CompareAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var mismatches = new List<MismatchRow>();

        var srcDirs = (await _scanner.GetDirectoriesAsync(sourcePath, cancellationToken))
            .Select(d => d.Name);
        var dstDirs = (await _scanner.GetDirectoriesAsync(destinationPath, cancellationToken))
            .Select(d => d.Name);

        var allDirs = new HashSet<string>(srcDirs, StringComparer.OrdinalIgnoreCase);
        foreach (var d in dstDirs)
        {
            allDirs.Add(d);
        }

        foreach (var dir in allDirs.Append(string.Empty))
        {
            var srcDir = Path.Combine(sourcePath, dir);
            var dstDir = Path.Combine(destinationPath, dir);
            var srcExists = Directory.Exists(srcDir);
            var dstExists = Directory.Exists(dstDir);

            if (!srcExists || !dstExists)
            {
                mismatches.Add(new MissingEntryRow(dir, !srcExists));
                continue;
            }

            CompareFiles(srcDir, dstDir, dir, mismatches);
        }

        return mismatches;
    }

    private static void CompareFiles(string srcDir, string dstDir, string prefix, List<MismatchRow> mismatches)
    {
        var srcFiles = Directory.EnumerateFiles(srcDir).ToDictionary(Path.GetFileName, f => f, StringComparer.OrdinalIgnoreCase);
        var dstFiles = Directory.EnumerateFiles(dstDir).ToDictionary(Path.GetFileName, f => f, StringComparer.OrdinalIgnoreCase);
        foreach (var name in srcFiles.Keys.Union(dstFiles.Keys))
        {
            var srcExists = srcFiles.TryGetValue(name, out var sPath);
            var dstExists = dstFiles.TryGetValue(name, out var dPath);
            var rel = Path.Combine(prefix, name).TrimStart(Path.DirectorySeparatorChar);

            if (!srcExists)
            {
                mismatches.Add(new MissingEntryRow(rel, true));
            }
            else if (!dstExists)
            {
                mismatches.Add(new MissingEntryRow(rel, false));
            }
            else
            {
                var sSize = new FileInfo(sPath).Length;
                var dSize = new FileInfo(dPath).Length;
                if (sSize != dSize)
                {
                    mismatches.Add(new SizeMismatchRow(rel, sSize, dSize));
                }
            }
        }
    }
}
