using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsPipeline.Core;

/// <summary>
/// Recursively scans directories using an <see cref="IDriveScanner"/>.
/// Each worker pulls items from a queue so scanning can proceed concurrently.
/// </summary>
public class DirectoryScanner
{
    private readonly IDriveScanner _scanner;
    private readonly ILogger<DirectoryScanner> _logger;
    private readonly int _maxConcurrency;

    public DirectoryScanner(IDriveScanner scanner, ILogger<DirectoryScanner> logger, int maxConcurrency = 4)
    {
        _scanner = scanner;
        _logger = logger;
        _maxConcurrency = maxConcurrency;
    }

    /// <summary>
    /// Walks the directory tree starting from <paramref name="rootId"/>.
    /// </summary>
    /// <param name="rootId">Root directory identifier.</param>
    /// <param name="rootPath">Path label used for dictionary keys.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    public async Task<IDictionary<string, DirectoryCounts>> ScanAsync(string rootId, string rootPath, CancellationToken cancellationToken = default)
    {
        var map = new ConcurrentDictionary<string, DirectoryCounts>();
        var queue = new ConcurrentQueue<(string Id, string Path)>();
        queue.Enqueue((rootId, rootPath));

        var workers = new List<Task>();
        for (int i = 0; i < _maxConcurrency; i++)
        {
            workers.Add(Task.Run(async () =>
            {
                while (queue.TryDequeue(out var item) && !cancellationToken.IsCancellationRequested)
                {
                    var counts = await _scanner.GetCountsAsync(item.Id, cancellationToken);
                    map[item.Path] = counts;

                    var children = await _scanner.GetDirectoriesAsync(item.Id, cancellationToken);
                    foreach (var child in children)
                    {
                        var childPath = string.IsNullOrEmpty(item.Path) ? child.Name : $"{item.Path}/{child.Name}";
                        queue.Enqueue((child.Id, childPath));
                    }
                }
            }, cancellationToken));
        }

        await Task.WhenAll(workers);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Scanned {count} directories", map.Count);
        }
        return map;
    }
}

