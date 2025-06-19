using System.Collections.Generic;
using System.Threading.Tasks;
using File = Google.Apis.Drive.v3.Data.File;
using FluentAssertions;
using MetricsPipeline.Core;
using Xunit;

namespace MetricsPipeline.Core.Tests;

public class ScannerCountsTests
{
    [Fact]
    public async Task GoogleScanner_ReturnsCorrectCounts()
    {
        var items = new List<File>
        {
            new File { MimeType = "application/vnd.google-apps.folder" },
            new File { MimeType = "text/plain", Size = 5 }
        };
        var scanner = new TestGoogleDriveScanner(items);
        var counts = await scanner.GetCountsAsync("id");
        counts.FileCount.Should().Be(1);
        counts.DirectoryCount.Should().Be(1);
        counts.TotalBytes.Should().Be(5);
    }

    [Fact]
    public async Task GraphScanner_ReturnsCorrectCounts()
    {
        var items = new List<Microsoft.Graph.Models.DriveItem>
        {
            new Microsoft.Graph.Models.DriveItem { Folder = new Microsoft.Graph.Models.Folder() },
            new Microsoft.Graph.Models.DriveItem { File = new Microsoft.Graph.Models.FileObject(), Size = 10 }
        };
        var scanner = new TestGraphScanner(items);
        var counts = await scanner.GetCountsAsync("id:item");
        counts.FileCount.Should().Be(1);
        counts.DirectoryCount.Should().Be(1);
        counts.TotalBytes.Should().Be(10);
    }
}
