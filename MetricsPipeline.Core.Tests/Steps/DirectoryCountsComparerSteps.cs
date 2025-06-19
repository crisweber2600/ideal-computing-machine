using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Reqnroll;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class DirectoryCountsComparerSteps
{
    private Dictionary<string, DirectoryCounts>? _left;
    private Dictionary<string, DirectoryCounts>? _right;
    private List<CountsDifference>? _result;

    [Given("two maps with counts")]
    public void GivenTwoMapsWithCounts()
    {
        _left = new Dictionary<string, DirectoryCounts>
        {
            ["a"] = new DirectoryCounts(1, 0, 0),
            ["b"] = new DirectoryCounts(2, 0, 0)
        };
        _right = new Dictionary<string, DirectoryCounts>
        {
            ["a"] = new DirectoryCounts(1, 0, 0),
            ["b"] = new DirectoryCounts(1, 0, 0)
        };
    }

    [When("I compare the maps")]
    public void WhenICompareTheMaps()
    {
        var comparer = new DirectoryCountsComparer();
        _result = comparer.Compare(_left!, _right!).ToList();
    }

    [Then("only differing paths should be returned")]
    public void ThenOnlyDifferingPathsShouldBeReturned()
    {
        _result.Should().ContainSingle();
        _result![0].Path.Should().Be("b");
    }
}
