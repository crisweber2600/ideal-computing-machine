using Microsoft.Extensions.Logging;
using MetricsPipeline.Core;

namespace MetricsCli;

public static class PipelineRunner
{
    public static async Task RunAsync(Options options,
        IDriveScanner googleScanner,
        IDriveScanner microsoftScanner,
        Stream output,
        ILoggerFactory loggerFactory)
    {
        var gScanner = new DirectoryScanner(googleScanner, loggerFactory.CreateLogger<DirectoryScanner>());
        var mScanner = new DirectoryScanner(microsoftScanner, loggerFactory.CreateLogger<DirectoryScanner>());
        var googleCounts = await gScanner.ScanAsync(options.GoogleRoot, options.GoogleRoot);
        var microsoftCounts = await mScanner.ScanAsync(options.MsRoot, options.MsRoot);

        var comparer = new DirectoryCountsComparer();
        var differences = comparer.Compare((IReadOnlyDictionary<string, DirectoryCounts>)googleCounts,
            (IReadOnlyDictionary<string, DirectoryCounts>)microsoftCounts);
        var exporter = new CsvExporter();
        await exporter.ExportAsync(differences, output);
    }
}

