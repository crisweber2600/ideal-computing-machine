using System.IO;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Reqnroll;
using FluentAssertions;
using MetricsCli;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class CommandLineSteps
{
    private PipelineOptions? _options;
    private string? _csv;

    [Given("environment variable GOOGLE_AUTH is set to \"(.*)\"")]
    public void GivenEnvIsSet(string path)
    {
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", path);
    }

    [Given("MS_ROOT is set to \"(.*)\"")]
    public void GivenMsRoot(string value)
    {
        Environment.SetEnvironmentVariable("MS_ROOT", value);
    }

    [Given("GOOGLE_ROOT is set to \"(.*)\"")]
    public void GivenGoogleRoot(string value)
    {
        Environment.SetEnvironmentVariable("GOOGLE_ROOT", value);
    }

    [Given("OUTPUT_CSV is set to \"(.*)\"")]
    public void GivenOutputCsv(string value)
    {
        Environment.SetEnvironmentVariable("OUTPUT_CSV", value);
    }

    [Given("MAX_DOP is set to \"(.*)\"")]
    public void GivenMaxDop(string value)
    {
        Environment.SetEnvironmentVariable("MAX_DOP", value);
    }

    [When("I parse the arguments \"(.*)\"")]
    public void WhenIParse(string args)
    {
        _options = Program.ParseOptions(args.Split(' '));
    }

    [Then("the parsed options should contain \"(.*)\"")]
    public void ThenOptionsShouldContain(string expected)
    {
        _options!.GoogleAuth.Should().Be(expected);
    }

    [Then("the options MsRoot should be \"(.*)\"")]
    public void ThenMsRoot(string expected)
    {
        _options!.MsRoot.Should().Be(expected);
    }

    [Then("the options GoogleRoot should be \"(.*)\"")]
    public void ThenGoogleRoot(string expected)
    {
        _options!.GoogleRoot.Should().Be(expected);
    }

    [Then("the output path should be \"(.*)\"")]
    public void ThenOutputPath(string expected)
    {
        _options!.Output.Should().Be(expected);
    }

    [Then("the max dop should be (\\d+)")]
    public void ThenMaxDop(int expected)
    {
        _options!.MaxDop.Should().Be(expected);
    }

    [When("I run the CLI pipeline")]
    public async Task WhenIRunPipeline()
    {
        var google = new CliStubScanner(new Dictionary<string, DirectoryCounts> { ["g"] = new DirectoryCounts(1,0,0) });
        var ms = new CliStubScanner(new Dictionary<string, DirectoryCounts> { ["m"] = new DirectoryCounts(0,1,0) });
        using var stream = new MemoryStream();
        var loggerFactory = LoggerFactory.Create(b => { });
        var options = new PipelineOptions("m","g","out.csv","cred.json",1,false);
        await PipelineRunner.RunAsync(options, google, ms, stream, loggerFactory);
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen:true);
        _csv = await reader.ReadToEndAsync();
    }

    [Then("the output should contain a CSV header")]
    public void ThenOutputContainsHeader()
    {
        _csv!.Split('\n')[0].Should().Contain("Path");
    }
}

internal sealed class CliStubScanner : IDriveScanner
{
    private readonly IReadOnlyDictionary<string, DirectoryCounts> _map;

    public CliStubScanner(IDictionary<string, DirectoryCounts> map)
    {
        _map = new Dictionary<string, DirectoryCounts>(map);
    }

    public Task<IEnumerable<DirectoryEntry>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<DirectoryEntry>>(Array.Empty<DirectoryEntry>());

    public Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(_map[path]);
}
