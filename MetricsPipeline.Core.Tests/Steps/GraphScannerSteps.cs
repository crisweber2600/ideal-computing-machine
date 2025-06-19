using Moq;
using Reqnroll;
using MetricsPipeline.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using FluentAssertions;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class GraphScannerSteps
{
    private IEnumerable<string>? _result;

    [Given("a drive contains two child folders")]
    public void GivenADriveContainsTwoChildFolders()
    {
        // Items are configured in When step using TestGraphScanner
    }

    [When("I request the list of directories")]
    public async Task WhenIRequestTheListOfDirectories()
    {
        var items = new List<DriveItem>
        {
            new DriveItem { Name = "one", Folder = new Folder() },
            new DriveItem { Name = "two", Folder = new Folder() }
        };
        var scanner = new TestGraphScanner(items);
        _result = await scanner.GetDirectoriesAsync("id:item");
    }

    [Then("both folder names should be returned")]
    public void ThenBothFolderNamesShouldBeReturned()
    {
        _result.Should().Contain(new[] { "one", "two" });
    }
}
