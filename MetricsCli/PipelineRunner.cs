using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MetricsPipeline.Core;
using MetricsPipeline.Core.Infrastructure.Workers;

namespace MetricsCli;

public static class PipelineRunner
{
    public static async Task RunAsync(Options options,
        IDriveScanner googleScanner,
        IDriveScanner microsoftScanner,
        Stream output,
        ILoggerFactory loggerFactory)
    {
        var googleCounts = new ConcurrentDictionary<string, DirectoryCounts>();
        var microsoftCounts = new ConcurrentDictionary<string, DirectoryCounts>();
        var worker = new CliCoordinatorWorker(googleScanner, microsoftScanner,
            new[]{(options.GoogleRoot, options.MsRoot)},
            googleCounts, microsoftCounts,
            loggerFactory.CreateLogger<MultiDriveCoordinatorWorker>());
        await worker.RunAsync();

        var comparer = new DirectoryCountsComparer();
        var differences = comparer.Compare(googleCounts, microsoftCounts);
        var exporter = new CsvExporter();
        await exporter.ExportAsync(differences, output);
    }
}

internal sealed class CliCoordinatorWorker : MultiDriveCoordinatorWorker
{
    public CliCoordinatorWorker(IDriveScanner google,
        IDriveScanner microsoft,
        IEnumerable<(string GoogleRoot, string MicrosoftRoot)> roots,
        ConcurrentDictionary<string, DirectoryCounts> gMap,
        ConcurrentDictionary<string, DirectoryCounts> mMap,
        ILogger<MultiDriveCoordinatorWorker> logger)
        : base(google, microsoft, roots, gMap, mMap, logger)
    {
    }

    public Task RunAsync() => base.ExecuteAsync(CancellationToken.None);
}
