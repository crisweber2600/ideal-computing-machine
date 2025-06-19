using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MetricsPipeline.Core;
using MetricsPipeline.Core.Infrastructure.Workers;

namespace MetricsPipeline.Core.Tests;

public class DriveScannerWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_LogsCountsForEachDirectory()
    {
        var scanner = new Mock<IDriveScanner>();
        scanner.Setup(s => s.GetDirectoriesAsync("root", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { new DirectoryEntry("child","child", null) });
        scanner.Setup(s => s.GetCountsAsync("child", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new DirectoryCounts(1,0,0));

        var logger = new Mock<ILogger<DriveScannerWorker>>();
        logger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);
        var worker = new DriveScannerWorker(scanner.Object, logger.Object, "root");

        var method = typeof(DriveScannerWorker).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var task = (Task)method.Invoke(worker, new object[] { CancellationToken.None })!;
        await task;

        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("child")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once());
    }

}
