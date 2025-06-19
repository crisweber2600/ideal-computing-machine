using System.Collections.Concurrent;
using Reqnroll;
using MetricsPipeline.Core;
using MetricsPipeline.Core.Infrastructure.Workers;
using FluentAssertions;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class MultiDriveCoordinatorSteps
{
    private readonly ConcurrentDictionary<string, DirectoryCounts> _google = new();
    private readonly ConcurrentDictionary<string, DirectoryCounts> _microsoft = new();
    private IEnumerable<(string GoogleRoot, string MicrosoftRoot)>? _pairs;

    [Given("two root pairs exist")]
    public void GivenTwoRootPairsExist()
    {
        _pairs = new List<(string, string)>
        {
            ("g1", "m1"),
            ("g2", "m2")
        };
    }

    [When("the coordinator processes the queue")]
    public async Task WhenTheCoordinatorProcessesTheQueue()
    {
        var googleScanner = new StubScanner(new Dictionary<string, DirectoryCounts>
        {
            ["g1"] = new DirectoryCounts(1, 0, 0),
            ["g2"] = new DirectoryCounts(1, 0, 0)
        });
        var microsoftScanner = new StubScanner(new Dictionary<string, DirectoryCounts>
        {
            ["m1"] = new DirectoryCounts(2, 0, 0),
            ["m2"] = new DirectoryCounts(2, 0, 0)
        });
        var worker = new TestCoordinatorWorker(
            googleScanner,
            microsoftScanner,
            _pairs!,
            _google,
            _microsoft);
        await worker.RunAsync();
    }

    [Then("counts for all roots should be recorded")]
    public void ThenCountsForAllRootsShouldBeRecorded()
    {
        _google.Should().HaveCount(2);
        _microsoft.Should().HaveCount(2);
    }
}

internal sealed class StubScanner : IDriveScanner
{
    private readonly IReadOnlyDictionary<string, DirectoryCounts> _data;

    public StubScanner(IDictionary<string, DirectoryCounts> data)
    {
        _data = new Dictionary<string, DirectoryCounts>(data);
    }

    public Task<IEnumerable<string>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

    public Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(_data[path]);
}

internal sealed class TestCoordinatorWorker : MultiDriveCoordinatorWorker
{
    public TestCoordinatorWorker(
        IDriveScanner google,
        IDriveScanner microsoft,
        IEnumerable<(string GoogleRoot, string MicrosoftRoot)> roots,
        ConcurrentDictionary<string, DirectoryCounts> googleMap,
        ConcurrentDictionary<string, DirectoryCounts> microsoftMap)
        : base(google, microsoft, roots, googleMap, microsoftMap, new NullLogger<MultiDriveCoordinatorWorker>())
    {
    }

    public Task RunAsync() => base.ExecuteAsync(CancellationToken.None);
}
