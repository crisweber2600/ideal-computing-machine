using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.Kiota.Abstractions.Authentication;

namespace MetricsPipeline.Core.Tests;

public class TestGraphScanner : GraphScanner
{
    private readonly IEnumerable<DriveItem> _items;

    public TestGraphScanner(IEnumerable<DriveItem> items)
        : base(new GraphServiceClient(new Mock<IAuthenticationProvider>().Object, "https://graph.microsoft.com/v1.0"), new NullLogger<GraphScanner>())
    {
        _items = items;
    }

    protected override async IAsyncEnumerable<DriveItem> GetChildrenAsync(string driveId, string itemId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in _items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}

public class NullLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    private class NullDisposable : IDisposable { public static readonly IDisposable Instance = new NullDisposable(); public void Dispose() { } }
}
