using Google.Apis.Drive.v3;
using File = Google.Apis.Drive.v3.Data.File;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Net;

namespace MetricsPipeline.Core;

/// <summary>
/// Scans a Google Drive folder using the Drive API.
/// </summary>
public class GoogleDriveScanner : IDriveScanner
{
    private readonly DriveService _service;
    private readonly ILogger<GoogleDriveScanner> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly bool _followShortcuts;

    public GoogleDriveScanner(DriveService service, ILogger<GoogleDriveScanner> logger, bool followShortcuts = false, int maxConcurrency = 4)
    {
        _service = service;
        _logger = logger;
        _followShortcuts = followShortcuts;
        _semaphore = new SemaphoreSlim(maxConcurrency);
        _retryPolicy = Policy
            .Handle<Google.GoogleApiException>(ex => ex.HttpStatusCode == HttpStatusCode.TooManyRequests ||
                                                 ex.HttpStatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, ctx) =>
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(ex, "Drive request throttled. Waiting {Delay} before retry", delay);
                    }
                });
    }

    public async Task<IEnumerable<DirectoryEntry>> GetDirectoriesAsync(string rootId, CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<DirectoryEntry>();
        await foreach (var file in GetChildrenAsync(rootId, cancellationToken))
        {
            if (IsDirectory(file))
            {
                results.Add(new DirectoryEntry(file.Id!, file.Name ?? string.Empty));
            }
        }
        return results.ToArray();
    }

    public async Task<DirectoryCounts> GetCountsAsync(string path, CancellationToken cancellationToken = default)
    {
        int files = 0;
        int dirs = 0;
        long bytes = 0;

        await foreach (var file in GetChildrenAsync(path, cancellationToken))
        {
            if (IsDirectory(file))
            {
                dirs++;
            }
            else
            {
                files++;
                bytes += file.Size ?? 0;
            }
        }

        return new DirectoryCounts(files, dirs, bytes);
    }

    protected virtual async IAsyncEnumerable<File> GetChildrenAsync(string folderId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? pageToken = null;
        do
        {
            var request = _service.Files.List();
            request.Q = $"'{folderId}' in parents and trashed=false";
            request.Fields = "nextPageToken, files(id,name,mimeType,shortcutDetails,targetId,size,shortcutDetails/targetMimeType)";
            request.PageToken = pageToken;
            var response = await ExecuteWithRetry(() => request.ExecuteAsync(cancellationToken));
            if (response.Files != null)
            {
                foreach (var file in response.Files)
                {
                    yield return file;
                }
            }
            pageToken = response.NextPageToken;
        } while (pageToken != null);
    }

    private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _retryPolicy.ExecuteAsync(() => operation());
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool IsDirectory(File file)
    {
        if (file.MimeType == "application/vnd.google-apps.folder")
            return true;
        if (_followShortcuts && file.MimeType == "application/vnd.google-apps.shortcut" &&
            file.ShortcutDetails?.TargetMimeType == "application/vnd.google-apps.folder")
            return true;
        return false;
    }
}
