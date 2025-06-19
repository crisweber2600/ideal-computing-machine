using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Reqnroll;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class DirectoryComparerSteps : IDisposable
{
    private readonly string _root;
    private readonly string _source;
    private readonly string _destination;
    private List<MismatchRow>? _rows;

    public DirectoryComparerSteps()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _source = Path.Combine(_root, "src");
        _destination = Path.Combine(_root, "dst");
        Directory.CreateDirectory(_source);
        Directory.CreateDirectory(_destination);
    }

    [Given(@"the source directory contains ""(.*)"" with (\d+) bytes")]
    public void GivenSourceFile(string name, int bytes)
    {
        File.WriteAllBytes(Path.Combine(_source, name), new byte[bytes]);
    }

    [Given(@"the destination directory contains ""(.*)"" with (\d+) bytes")]
    public void GivenDestinationFile(string name, int bytes)
    {
        File.WriteAllBytes(Path.Combine(_destination, name), new byte[bytes]);
    }

    [When("I compare the source and destination directories")]
    public async Task WhenICompare()
    {
        var logger = new NullLogger<DirectoryScanner>();
        var stub = new LocalScanner();
        var scanner = new DirectoryScanner(stub, logger);
        var comparer = new DirectoryComparer(stub);

        _rows = (await comparer.CompareAsync(_source, _destination)).ToList();
    }

    [Then("two mismatches should be reported")]
    public void ThenTwoMismatches()
    {
        _rows.Should().HaveCount(2);
        _rows.Should().ContainSingle(r => r is MissingEntryRow);
        _rows.Should().ContainSingle(r => r is SizeMismatchRow);
    }

public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }
}

internal sealed class LocalScanner : IDriveScanner
{
    public Task<IEnumerable<DirectoryEntry>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var entries = Directory.EnumerateDirectories(rootPath)
            .Select(d => new DirectoryEntry(d, Path.GetFileName(d)));
        return Task.FromResult<IEnumerable<DirectoryEntry>>(entries.ToArray());
    }

    public Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
    {
        var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        var dirs = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories);
        long bytes = files.Sum(f => new FileInfo(f).Length);
        return Task.FromResult(new DirectoryCounts(files.Count(), dirs.Count(), bytes));
    }
}
