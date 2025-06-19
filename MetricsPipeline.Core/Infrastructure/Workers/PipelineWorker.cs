using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Infrastructure.Workers;

/// <summary>
/// Runs the comparison pipeline once using the provided options and scanners.
/// </summary>
public sealed class PipelineWorker : BackgroundService
{
    private readonly IDriveScanner _google;
    private readonly IDriveScanner _microsoft;
    private readonly PipelineOptions _options;
    private readonly ILoggerFactory _loggerFactory;

    public PipelineWorker(GoogleDriveScanner google,
        GraphScanner microsoft,
        PipelineOptions options,
        ILoggerFactory loggerFactory)
    {
        _google = google;
        _microsoft = microsoft;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var stream = File.Create(_options.Output);
        await PipelineRunner.RunAsync(_options, _google, _microsoft, stream, _loggerFactory, stoppingToken);
    }
}
