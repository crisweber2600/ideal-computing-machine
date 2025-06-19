using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MetricsPipeline.Core.Infrastructure.Workers;

/// <summary>
/// Coordinates scanning of multiple root directory pairs across Google and Microsoft drives.
/// </summary>
public class MultiDriveCoordinatorWorker : BackgroundService
{
    private readonly IDriveScanner _googleScanner;
    private readonly IDriveScanner _microsoftScanner;
    private readonly IEnumerable<(string GoogleRoot, string MicrosoftRoot)> _roots;
    private readonly ConcurrentDictionary<string, DirectoryCounts> _googleCounts;
    private readonly ConcurrentDictionary<string, DirectoryCounts> _microsoftCounts;
    private readonly ILogger<MultiDriveCoordinatorWorker> _logger;

    public MultiDriveCoordinatorWorker(
        IDriveScanner googleScanner,
        IDriveScanner microsoftScanner,
        IEnumerable<(string GoogleRoot, string MicrosoftRoot)> roots,
        ConcurrentDictionary<string, DirectoryCounts> googleCounts,
        ConcurrentDictionary<string, DirectoryCounts> microsoftCounts,
        ILogger<MultiDriveCoordinatorWorker> logger)
    {
        _googleScanner = googleScanner;
        _microsoftScanner = microsoftScanner;
        _roots = roots;
        _googleCounts = googleCounts;
        _microsoftCounts = microsoftCounts;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queue = new ConcurrentQueue<(string GoogleRoot, string MicrosoftRoot)>(_roots);
        var workers = new List<Task>();
        var workerCount = Environment.ProcessorCount * 2;

        for (int i = 0; i < workerCount; i++)
        {
            workers.Add(Task.Run(async () =>
            {
                while (queue.TryDequeue(out var pair) && !stoppingToken.IsCancellationRequested)
                {
                    var gScanner = new DirectoryScanner(_googleScanner, Microsoft.Extensions.Logging.Abstractions.NullLogger<DirectoryScanner>.Instance);
                    var gMap = await gScanner.ScanAsync(pair.GoogleRoot, pair.GoogleRoot, stoppingToken);
                    foreach (var kvp in gMap)
                    {
                        _googleCounts[kvp.Key] = kvp.Value;
                    }

                    var mScanner = new DirectoryScanner(_microsoftScanner, Microsoft.Extensions.Logging.Abstractions.NullLogger<DirectoryScanner>.Instance);
                    var mMap = await mScanner.ScanAsync(pair.MicrosoftRoot, pair.MicrosoftRoot, stoppingToken);
                    foreach (var kvp in mMap)
                    {
                        _microsoftCounts[kvp.Key] = kvp.Value;
                    }
                }
            }, stoppingToken));
        }

        await Task.WhenAll(workers);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Aggregated {google} Google and {microsoft} Microsoft entries", _googleCounts.Count, _microsoftCounts.Count);
        }
    }
}
