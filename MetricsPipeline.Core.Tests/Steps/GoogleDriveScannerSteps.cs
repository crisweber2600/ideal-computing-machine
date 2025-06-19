using Moq;
using Reqnroll;
using MetricsPipeline.Core;
using File = Google.Apis.Drive.v3.Data.File;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class GoogleDriveScannerSteps
{
    private IEnumerable<DirectoryEntry>? _result;

    [Given("a drive folder contains a folder shortcut")]
    public void GivenAFolderShortcut()
    {
        // Items configured in When step using TestGoogleDriveScanner
    }

    [Given("a drive folder contains two child folders")]
    public void GivenADriveFolderContainsTwoChildFolders()
    {
        // Items configured in When step using TestGoogleDriveScanner
    }

    [When("I request the list of Google drive directories")]
    public async Task WhenIRequestTheListOfGoogleDriveDirectories()
    {
        var items = new List<File>
        {
            new File { Name = "one", MimeType = "application/vnd.google-apps.folder" },
            new File { Name = "two", MimeType = "application/vnd.google-apps.folder" }
        };
        var scanner = new TestGoogleDriveScanner(items);
        _result = await scanner.GetDirectoriesAsync("id");
    }

    [When("I request the list of Google drive directories with shortcut support")]
    public async Task WhenIRequestTheListOfGoogleDriveDirectoriesWithShortcutSupport()
    {
        var items = new List<File>
        {
            new File
            {
                Name = "link",
                MimeType = "application/vnd.google-apps.shortcut",
                ShortcutDetails = new File.ShortcutDetailsData { TargetMimeType = "application/vnd.google-apps.folder" }
            }
        };
        var scanner = new TestGoogleDriveScanner(items, true);
        _result = await scanner.GetDirectoriesAsync("id");
    }

    [Then("both Google folder names should be returned")]
    public void ThenBothGoogleFolderNamesShouldBeReturned()
    {
        _result!.Select(d => d.Name).Should().Contain(new[] { "one", "two" });
    }

    [Then("the shortcut folder name should be returned")]
    public void ThenTheShortcutFolderNameShouldBeReturned()
    {
        var names = _result!.Select(d => d.Name).ToList();
        names.Should().ContainSingle().Which.Should().Be("link");

    }
}
