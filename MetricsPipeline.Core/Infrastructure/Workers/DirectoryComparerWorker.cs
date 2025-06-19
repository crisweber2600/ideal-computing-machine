using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MetricsPipeline.Core.Infrastructure.Workers;

/// <summary>
/// Worker that compares two directories using <see cref="DirectoryComparer"/>.
/// </summary>
public sealed class DirectoryComparerWorker : BackgroundService
{
    private readonly IDriveScanner _scanner;
    private readonly ILogger<DirectoryComparerWorker> _logger;
    private readonly string _source;
    private readonly string _destination;

    public DirectoryComparerWorker(IDriveScanner scanner, ILogger<DirectoryComparerWorker> logger, string source, string destination)
    {
        _scanner = scanner;
        _logger = logger;
        _source = source;
        _destination = destination;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var comparer = new DirectoryComparer(_scanner);
        var mismatches = await comparer.CompareAsync(_source, _destination, stoppingToken);
        foreach (var mismatch in mismatches)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Mismatch: {path} - {type}", mismatch.RelativePath, mismatch.GetType().Name);
            }
        }
    }
}
