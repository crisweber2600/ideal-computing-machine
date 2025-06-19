using Microsoft.Extensions.Logging;
using MetricsPipeline.Core;

namespace MetricsCli;

public static class PipelineRunner
{
    public static async Task RunAsync(Options options,
        IDriveScanner googleScanner,
        IDriveScanner microsoftScanner,
        Stream output,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var gScanner = new DirectoryScanner(googleScanner, loggerFactory.CreateLogger<DirectoryScanner>(), options.MaxDop);
        var mScanner = new DirectoryScanner(microsoftScanner, loggerFactory.CreateLogger<DirectoryScanner>(), options.MaxDop);
        var googleCounts = await gScanner.ScanAsync(options.GoogleRoot, options.GoogleRoot, cancellationToken);
        var microsoftCounts = await mScanner.ScanAsync(options.MsRoot, options.MsRoot, cancellationToken);

        var comparer = new DirectoryCountsComparer();
        var differences = comparer.Compare((IReadOnlyDictionary<string, DirectoryCounts>)googleCounts,
            (IReadOnlyDictionary<string, DirectoryCounts>)microsoftCounts);
        var exporter = new CsvExporter();
        await exporter.ExportAsync(differences, output, cancellationToken);
    }
}

