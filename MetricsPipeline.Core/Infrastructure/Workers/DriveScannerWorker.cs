using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MetricsPipeline.Core.Infrastructure.Workers;

/// <summary>
/// Worker that scans a drive using <see cref="IDriveScanner"/> and logs the results.
/// </summary>
public sealed class DriveScannerWorker : BackgroundService
{
    private readonly IDriveScanner _scanner;
    private readonly ILogger<DriveScannerWorker> _logger;
    private readonly string _rootPath;

    public DriveScannerWorker(IDriveScanner scanner, ILogger<DriveScannerWorker> logger, string rootPath)
    {
        _scanner = scanner;
        _logger = logger;
        _rootPath = rootPath;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var directories = await _scanner.GetDirectoriesAsync(_rootPath, stoppingToken);
        foreach (var dir in directories)
        {
            var counts = await _scanner.GetCountsAsync(dir, stoppingToken);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("{dir} -> {files} files, {dirs} dirs", dir, counts.FileCount, counts.DirectoryCount);
            }
        }
    }
}
