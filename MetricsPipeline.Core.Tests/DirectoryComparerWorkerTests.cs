using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MetricsPipeline.Core;
using MetricsPipeline.Core.Infrastructure.Workers;

namespace MetricsPipeline.Core.Tests;

public sealed class DirectoryComparerWorkerTests : IDisposable
{
    private readonly string _root;
    private readonly string _src;
    private readonly string _dst;

    public DirectoryComparerWorkerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _src = Path.Combine(_root, "src");
        _dst = Path.Combine(_root, "dst");
        Directory.CreateDirectory(_src);
        Directory.CreateDirectory(_dst);
    }

    [Fact]
    public async Task ExecuteAsync_LogsAllMismatches()
    {
        File.WriteAllText(Path.Combine(_src, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_dst, "b.txt"), "b");
        File.WriteAllText(Path.Combine(_src, "c.txt"), "1");
        File.WriteAllText(Path.Combine(_dst, "c.txt"), "22");

        var logger = new Mock<ILogger<DirectoryComparerWorker>>();
        logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        var scanner = new FileSystemScanner();
        var worker = new DirectoryComparerWorker(scanner, logger.Object, _src, _dst);

        var method = typeof(DirectoryComparerWorker).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var task = (Task)method.Invoke(worker, new object[] { CancellationToken.None })!;
        await task;

        logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Mismatch")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(3));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private sealed class FileSystemScanner : IDriveScanner
    {
        public Task<IEnumerable<DirectoryEntry>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default)
        {
            var dirs = Directory.EnumerateDirectories(rootPath)
                .Select(d => new DirectoryEntry(d, Path.GetFileName(d), null));
            return Task.FromResult<IEnumerable<DirectoryEntry>>(dirs);
        }

        public Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult(new DirectoryCounts(0, 0, 0));
    }
}
