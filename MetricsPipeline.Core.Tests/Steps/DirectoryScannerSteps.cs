using System.Collections.Concurrent;
using Reqnroll;
using FluentAssertions;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class DirectoryScannerSteps
{
    private IDictionary<string, DirectoryCounts>? _result;

    [Given("a drive root with nested folders")]
    public void GivenADriveRootWithNestedFolders()
    {
        var counts = new Dictionary<string, DirectoryCounts>
        {
            ["root"] = new DirectoryCounts(0,2,0),
            ["c1"] = new DirectoryCounts(1,1,0),
            ["c1a"] = new DirectoryCounts(1,0,0),
            ["c2"] = new DirectoryCounts(0,0,0)
        };
        var children = new Dictionary<string, DirectoryEntry[]>
        {
            ["root"] = new[]{ new DirectoryEntry("c1","c1", null), new DirectoryEntry("c2","c2", null) },
            ["c1"] = new[]{ new DirectoryEntry("c1a","c1a", null) },
            ["c1a"] = Array.Empty<DirectoryEntry>(),
            ["c2"] = Array.Empty<DirectoryEntry>()
        };
        _scanner = new TreeScannerStub(counts, children);
    }

    private IDriveScanner? _scanner;

    [When("the directory scanner processes the root")]
    public async Task WhenTheDirectoryScannerProcessesTheRoot()
    {
        var logger = new NullLogger<DirectoryScanner>();
        var dirScanner = new DirectoryScanner(_scanner!, logger);
        _result = await dirScanner.ScanAsync("root", "root");
    }

    [Then("counts for every directory should be stored")]
    public void ThenCountsForEveryDirectoryShouldBeStored()
    {
        _result.Should().NotBeNull();
        _result!.Should().HaveCount(4);
        _result.Keys.Should().BeEquivalentTo(new[]{"root","root/c1","root/c1/c1a","root/c2"});
    }
}

internal sealed class TreeScannerStub : IDriveScanner
{
    private readonly IReadOnlyDictionary<string, DirectoryCounts> _counts;
    private readonly IReadOnlyDictionary<string, DirectoryEntry[]> _children;

    public TreeScannerStub(IDictionary<string, DirectoryCounts> counts, IDictionary<string, DirectoryEntry[]> children)
    {
        _counts = new Dictionary<string, DirectoryCounts>(counts);
        _children = new Dictionary<string, DirectoryEntry[]>(children);
    }

    public Task<IEnumerable<DirectoryEntry>> GetDirectoriesAsync(string rootId, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<DirectoryEntry>>(_children.TryGetValue(rootId, out var c) ? c : Array.Empty<DirectoryEntry>());

    public Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(_counts[path]);
}
