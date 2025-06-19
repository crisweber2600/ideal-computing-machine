using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsPipeline.Core;

/// <summary>
/// Scans the local file system for directory information.
/// </summary>
public class DirectoryScanner : IDriveScanner
{
    /// <inheritdoc />
    public Task<IEnumerable<string>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var dirs = Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories);
        return Task.FromResult<IEnumerable<string>>(dirs.ToArray());
    }

    /// <inheritdoc />
    public Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
    {
        var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        var dirs = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories);
        long bytes = files.Sum(f => new FileInfo(f).Length);
        return Task.FromResult(new DirectoryCounts(files.Count(), dirs.Count(), bytes));
    }
}
