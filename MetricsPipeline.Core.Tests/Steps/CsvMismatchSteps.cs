using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Reqnroll;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class CsvMismatchSteps
{
    private readonly IGoogleScanner _google;
    private readonly IMicrosoftScanner _microsoft;
    private readonly ComparisonContext _context;
    private readonly MemoryStream _stream;
    private readonly ILoggerFactory _loggerFactory;
    private string? _csv;

    public CsvMismatchSteps(
        IGoogleScanner google,
        IMicrosoftScanner microsoft,
        ComparisonContext context,
        MemoryStream stream,
        ILoggerFactory loggerFactory)
    {
        _google = google;
        _microsoft = microsoft;
        _context = context;
        _stream = stream;
        _loggerFactory = loggerFactory;
    }

    [Given(@"a google root returns a count of (\d+)")]
    public void GivenAGoogleRootReturnsACountOf(int count)
    {
        _context.GoogleMock
            .Setup(s => s.GetCountsAsync("g", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryCounts(count, 0, 0));
        _context.GoogleMock
            .Setup(s => s.GetDirectoriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DirectoryEntry>());
    }

    [Given(@"a microsoft root returns a count of (\d+)")]
    public void GivenAMicrosoftRootReturnsACountOf(int count)
    {
        _context.MicrosoftMock
            .Setup(s => s.GetCountsAsync("m", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryCounts(count, 0, 0));
        _context.MicrosoftMock
            .Setup(s => s.GetDirectoriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DirectoryEntry>());
    }

    [When("the comparison pipeline runs")]
    public async Task WhenTheComparisonPipelineRuns()
    {
        var options = new PipelineOptions("m", "g", "out.csv", "cred.json", 1, false);
        await PipelineRunner.RunAsync(options, _google, _microsoft, _stream, _loggerFactory);
        _stream.Position = 0;
        using var reader = new StreamReader(_stream);
        _csv = await reader.ReadToEndAsync();
    }

    [Then("the CSV should contain two difference rows")]
    public void ThenTheCsvShouldContainTwoDifferenceRows()
    {
        var lines = _csv!.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        lines.Should().HaveCount(3);
    }
}
