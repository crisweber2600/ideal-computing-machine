using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using Polly;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Net.Http;

namespace MetricsPipeline.Core;

/// <summary>
/// Scans a Microsoft Graph drive using paging with concurrency control.
/// </summary>
public class GraphScanner : IDriveScanner
{
    private readonly GraphServiceClient _client;
    private readonly ILogger<GraphScanner> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GraphScanner(GraphServiceClient client, ILogger<GraphScanner> logger, int maxConcurrency = 4)
    {
        _client = client;
        _logger = logger;
        _semaphore = new SemaphoreSlim(maxConcurrency);
        _retryPolicy = Policy
            .Handle<ServiceException>(ex => ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.TooManyRequests ||
                                            ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(5, (attempt, ctx) =>
            {
                if (ctx.TryGetValue("RetryAfter", out var val) && val is TimeSpan ts)
                {
                    return ts;
                }
                return TimeSpan.FromSeconds(Math.Pow(2, attempt));
            }, onRetryAsync: (ex, delay, attempt, ctx) =>
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "Graph request throttled. Waiting {Delay} before retry", delay);
                }
                return Task.CompletedTask;
            });
    }

    public async Task<IEnumerable<DirectoryEntry>> GetDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var ids = rootPath.Split(':');
        var driveId = ids[0];
        var itemId = ids.Length > 1 ? ids[1] : "root";

        var results = new ConcurrentBag<DirectoryEntry>();
        await foreach (var item in GetChildrenAsync(driveId, itemId, cancellationToken))
        {
            if (item.Folder != null)
            {
                results.Add(new DirectoryEntry($"{driveId}:{item.Id}", item.Name ?? string.Empty));
            }
        }
        return results.ToArray();
    }

    public async Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
    {
        var ids = path.Split(':');
        var driveId = ids[0];
        var itemId = ids.Length > 1 ? ids[1] : "root";

        int files = 0;
        int dirs = 0;
        long bytes = 0;

        await foreach (var item in GetChildrenAsync(driveId, itemId, cancellationToken))
        {
            if (item.Folder != null)
            {
                dirs++;
            }
            else if (item.File != null)
            {
                files++;
                bytes += item.Size ?? 0;
            }
        }

        return new DirectoryCounts(files, dirs, bytes);
    }

    protected virtual async IAsyncEnumerable<DriveItem> GetChildrenAsync(string driveId, string itemId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var page = await ExecuteWithRetry(() => _client.Drives[driveId].Items[itemId].Children.GetAsync(cancellationToken: cancellationToken));
        if (page?.Value != null)
        {
            foreach (var item in page.Value)
            {
                if (item.File != null)
                {
                    var hash = item.File.Hashes?.QuickXorHash;
                    if (hash != null)
                    {
                        item.AdditionalData ??= new Dictionary<string, object>();
                        item.AdditionalData["QuickXorHash"] = hash;
                    }
                }
                yield return item;
            }
        }
    }

    private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _retryPolicy.ExecuteAsync(async ctx =>
            {
                try
                {
                    var result = await operation();
                    if (result is BaseCollectionPaginationCountResponse resp && resp.AdditionalData.TryGetValue("@odata.nextLink", out var nextObj) && nextObj is string next)
                    {
                        ctx["NextLink"] = next;
                    }
                    return result;
                }
                catch (ServiceException ex) when (ex.ResponseHeaders?.RetryAfter != null)
                {
                    ctx["RetryAfter"] = ex.ResponseHeaders.RetryAfter.Delta ?? TimeSpan.FromSeconds(1);
                    throw;
                }
            }, new Context());
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
