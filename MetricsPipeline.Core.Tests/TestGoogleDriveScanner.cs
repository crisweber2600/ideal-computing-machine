using Google.Apis.Drive.v3;
using File = Google.Apis.Drive.v3.Data.File;
using MetricsPipeline.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Google.Apis.Services;

namespace MetricsPipeline.Core.Tests;

public class TestGoogleDriveScanner : GoogleDriveScanner
{
    private readonly IEnumerable<File> _items;

    public TestGoogleDriveScanner(IEnumerable<File> items)
        : base(new DriveService(new BaseClientService.Initializer()), new NullLogger<GoogleDriveScanner>())
    {
        _items = items;
    }

    protected override async IAsyncEnumerable<File> GetChildrenAsync(string folderId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in _items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
