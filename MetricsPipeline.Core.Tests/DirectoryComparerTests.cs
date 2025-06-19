using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests;

public class DirectoryComparerTests
{
    [Fact]
    public async Task CompareAsync_InvokesScannerForBothPaths()
    {
        var scanner = new Mock<IDriveScanner>();
        scanner.Setup(s => s.GetDirectoriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Array.Empty<DirectoryEntry>());

        var comparer = new DirectoryComparer(scanner.Object);

        using var tmp = new TempDir();
        var src = Path.Combine(tmp.Path, "src");
        var dst = Path.Combine(tmp.Path, "dst");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dst);

        await comparer.CompareAsync(src, dst);

        scanner.Verify(s => s.GetDirectoriesAsync(src, It.IsAny<CancellationToken>()), Times.Once());
        scanner.Verify(s => s.GetDirectoriesAsync(dst, It.IsAny<CancellationToken>()), Times.Once());
    }
}

internal sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, true);
    }
}
