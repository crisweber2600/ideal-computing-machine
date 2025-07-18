# Ideal Computing Machine

This repository demonstrates a minimal setup for a worker service using **.NET 9**.
The `DirectorySyncWorker` project starts as a simple logging stub. Replace it or
register other hosted services to perform real work such as directory scanning.
It now includes a reusable library called `MetricsPipeline.Core` that provides drive
scanning and directory comparison helpers.
A new `GraphScanner` leverages the Microsoft Graph SDK to enumerate OneDrive or
SharePoint document libraries. It automatically handles throttling and parallel
requests.
The library now also offers a `GoogleDriveScanner` for listing folders in
Google Drive. It shares the same concurrency limits and retry behaviour as the
Graph implementation and can optionally resolve shortcuts.

`DirectoryEntry` now exposes an optional `QuickXorHash` so callers can verify
file contents when hashes are available. `GraphScanner` automatically populates
this value for file items.

`DirectoryScanner` is a new helper that walks child folders using a work queue
and limits concurrency with a semaphore. It produces a map of counts for every
directory discovered.

A new `MultiDriveCoordinatorWorker` coordinates scanning of Google and
Microsoft roots in parallel. It uses a work queue seeded with pairs of root
paths and fans out workers based on the CPU count to maximise throughput.
Aggregated file counts for both platforms are stored in memory for later
processing or comparison. The new `DirectoryCountsComparer` can merge these maps and expose only mismatched paths
for further processing.

## Prerequisites
- .NET 9 SDK (install via `dotnet-install.sh` or from the official [download page](https://aka.ms/dotnet-download)) (preview)
- A Unix-like shell capable of running bash scripts
- Git for version control
- `Microsoft.Graph` NuGet package for Graph scanning features
- `Google.Apis.Drive.v3` package for Google Drive integration

## Quick Start
Run the setup script then build and test the solution:
```bash
bash dotnet-install.sh --version latest --channel 9.0
dotnet restore
dotnet test
```

## Usage
1. Restore dependencies with `dotnet restore`.
2. Build the solution using `dotnet build`.
3. Run the worker with `dotnet run --project DirectorySyncWorker`.
4. Execute tests and generate coverage: `dotnet test --collect:"XPlat Code Coverage"`.
5. Disable telemetry during builds by setting `DOTNET_CLI_TELEMETRY_OPTOUT=1`.
6. Build the core library alone via `dotnet build MetricsPipeline.Core` if desired.
7. To scan a drive programmatically, inject an `IDriveScanner` implementation and
   use the provided `DriveScannerWorker` for scheduled scans.
8. The test suite now uses Reqnroll for BDD scenarios. Run `dotnet test` to
   execute feature files and unit tests.
9. Unit tests reside in the `MetricsPipeline.Core.Tests` project and verify
   `GraphScanner` behaviour.
10. `GoogleDriveScanner` can be used to enumerate folders from Google Drive. Use
    the `--follow-shortcuts` option to resolve shortcut targets automatically.
11. Use `MultiDriveCoordinatorWorker` to process Google and Microsoft roots
    concurrently. Seed it with tuples of root IDs and it will fan out scanning
    tasks according to your CPU count.
12. A new feature file validates the coordinator behaviour with BDD tests,
    ensuring counts are aggregated correctly.
13. DirectoryScanner is also exercised via a scenario that confirms nested
    folders are counted individually.
14. When running inside a minimal container you may set
    `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` to suppress locale warnings.
15. Use `DirectoryCountsComparer` to join Google and Microsoft maps and spot count mismatches.
16. `CsvExporter` streams these results directly to disk or stdout using `StreamWriter`.
17. A new feature file exercises the comparer so coverage remains high.
18. Example scripts now show how to pipe mismatches to a CSV file.
19. The README clarifies installing the .NET 9 preview SDK for this project.
20. The new `MetricsCli` tool runs the comparison pipeline from the command line.
21. Provide Microsoft and Google root IDs via `--ms-root` and `--google-root`.
22. Pass Google credentials with `--google-auth` or set the `GOOGLE_AUTH` environment variable.
23. Use `--output` to write mismatches to a CSV file.
24. Limit concurrency with the `--max-dop` option.
25. Step definitions now resolve services via Microsoft.Extensions.DependencyInjection.
26. `ScenarioDependencies` registers mocks for pipeline BDD tests.
27. A new feature checks that only mismatched entries reach the CSV export.
28. Moq supplies scanner stubs so tests remain fast and isolated.

29. Run `dotnet test --collect:"XPlat Code Coverage"` to verify coverage above 80%.
30. Configure OAuth credentials for Microsoft and Google before running scanners.
31. The CLI now supports environment variables for secret management.
32. `DirectoryEntry` includes a `QuickXorHash` for integrity checks.
33. `GraphScanner` populates this hash for file items automatically.
34. `DirectoryComparer` now uses directory names from these entries.
35. A local `FileSystemScanner` test helper demonstrates implementing `IDriveScanner`.
36. BDD tests were updated to assert on entry names rather than objects.
37. Coverage now exceeds 80% with additional unit tests for workers and scanners.
38. `DirectorySyncWorker` is only a logging stub. Register `MultiDriveCoordinatorWorker`
    if you want background scans.
39. Set `MS_ROOT` and `GOOGLE_ROOT` environment variables to provide the pair of
    drive IDs processed by the coordinator.
40. Provide an `OUTPUT_CSV` environment variable for the CLI to override the
    default `mismatches.csv` path.
41. Install the `reportgenerator` global tool to view coverage results:
    `dotnet tool install --global dotnet-reportgenerator-globaltool`.
42. Publish the CLI for reuse with `dotnet publish -c Release MetricsCli`.
43. Set `MAX_DOP` to control API concurrency without editing the source code.
44. CLI options now fall back to `MS_ROOT`, `GOOGLE_ROOT`, `OUTPUT_CSV` and
    `MAX_DOP` when corresponding switches are omitted.
45. `--max-dop` and `MAX_DOP` also control `DirectoryScanner` concurrency so
    scanning performance matches your environment.
46. `PipelineRunner.RunAsync` accepts a `CancellationToken` for graceful
    termination on Ctrl+C.
47. New BDD scenarios verify environment variable parsing for these options.
48. Minor cleanup removed duplicate using directives in `DirectoryScanner`.
49. `PipelineRunner` now lives in the core library so other projects can invoke it.
50. A `PipelineWorker` executes the entire comparison when configured.
51. Populate the `Pipeline` section in `appsettings.json` with `MsRoot`, `GoogleRoot`, `GoogleAuth`, `Output` and `MaxDop`.
52. The worker uses these settings to spin up scanners and export a CSV automatically.
53. Run `dotnet run --project DirectorySyncWorker` after updating the settings to process both drives end to end.

54. `PipelineOptions` now includes a parameterless constructor so it can be bound directly from configuration using `IOptions<PipelineOptions>`.
55. Set `MAX_DOP` or edit `appsettings.json` to tune concurrency without recompiling.
56. Mount your `appsettings.json` when running in Docker so the worker picks up the correct pipeline configuration.
57. Remember to pass `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` in container environments to silence locale warnings.
58. If you encounter a `MissingMethodException` for `PipelineOptions`, clean and rebuild the solution to ensure the latest binaries are used.
59. `EnvironmentValidator` checks that required environment variables are present before any scans run.
60. `MetricsCli` prints clear errors when `MS_ROOT`, `GOOGLE_ROOT` or `GOOGLE_AUTH` are missing.
61. The validator confirms the Google credentials file exists and reports the path when it does not.
62. Azure authentication requires `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_CLIENT_SECRET`.
63. The CLI exits early if validation fails so you can fix the configuration and retry.
64. A missing credentials file triggers "Google credentials not found"; ensure the path in `GOOGLE_AUTH` is correct.
65. If Google APIs return a 403 error, confirm the service account has access to the target Drive or folder.
66. Graph requests failing with 403 usually mean `Files.Read.All` or `Sites.Read.All` consent was not granted.
67. Azure-related validation errors indicate one of `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` or `AZURE_CLIENT_SECRET` is unset.
68. Use `--google-root root` to scan your own My Drive; for a Shared Drive supply its root ID instead.
```csharp
if (!EnvironmentValidator.Validate(out var errors))
{
    foreach (var e in errors)
        Console.Error.WriteLine(e);
    return;
}
```
Example `appsettings.json` configuration:
```json
{
  "Pipeline": {
    "MsRoot": "<drive-id>",
    "GoogleRoot": "<folder-id>",
    "GoogleAuth": "creds.json",
    "Output": "mismatches.csv",
    "MaxDop": 4,
    "FollowShortcuts": false
  }
}
```



## OAuth Configuration

### Microsoft Graph
1. Register an application in Azure Active Directory and grant `Files.Read.All` and
   `Sites.Read.All` API permissions.
2. Create a client secret and set `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and
   `AZURE_CLIENT_SECRET` environment variables.
3. The sample uses `DefaultAzureCredential` so tokens are acquired automatically
   when these variables are present or you are logged in with `Azure CLI`.

### Google Drive
1. Create a Google Cloud project and enable the Drive API.
2. Generate a service account key or OAuth client credentials and download the
   JSON file.
3. Provide the file path via `--google-auth` or set the `GOOGLE_AUTH`
   environment variable so the scanners can authenticate.


### Graph Scanning Example
```csharp
var credential = new DefaultAzureCredential();
var graphClient = new GraphServiceClient(credential);
var scanner = new GraphScanner(graphClient, logger);
var folders = await scanner.GetDirectoriesAsync("{driveId}:{rootItemId}");
```

The scanner restricts concurrency with `SemaphoreSlim` and retries 429 responses
using Polly's `WaitAndRetryAsync` policy.

### Google Drive Scanning Example
```csharp
var service = new DriveService(new BaseClientService.Initializer());
var scanner = new GoogleDriveScanner(service, logger, followShortcuts: true);
var folders = await scanner.GetDirectoriesAsync("{folderId}");
```
applies exponential back-off when the Drive API returns 429 or 503 errors.
### Local File System Example
```csharp
var scanner = new FileSystemDriveScanner("/tmp", logger);
var counts = await scanner.GetCountsAsync("/");
```


This solution serves as a starting point for building background services.
Refer to the [.NET 9 release notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9) for new features.
Feel free to extend it with your own business logic and tests.

## Project Structure
- **DirectorySyncWorker** – executable worker service.
- **MetricsPipeline.Core** – contains interfaces like `IDriveScanner` and
  `IDirectoryComparer` plus model records for comparison results.
- Worker classes reside under `MetricsPipeline.Core/Infrastructure/Workers` so
  they can be shared across services.
The library includes several workers:
- **DriveScannerWorker** – schedules background scans for a single platform.
- **DirectoryComparerWorker** – compares two local folders and logs mismatches.
- **MultiDriveCoordinatorWorker** – runs Microsoft and Google scans concurrently.
- **PipelineWorker** – executes the full comparison pipeline from configuration.


### Directory Scanning Example
```csharp
var scanner = serviceProvider.GetRequiredService<IDriveScanner>();
var counts = await scanner.GetCountsAsync("/data");
Console.WriteLine($"Files: {counts.FileCount}, Dirs: {counts.DirectoryCount}");
```

### Recursive Directory Map Example
```csharp
var dirScanner = new DirectoryScanner(scanner, logger);
var map = await dirScanner.ScanAsync("root-id", "root-name");
foreach (var (path, c) in map)
{
    Console.WriteLine($"{path} => {c.FileCount} files");
}
```

### Coordinated Drive Example
```csharp
var pairs = new[]{("gRoot","mRoot")};
var googleMap = new ConcurrentDictionary<string, DirectoryCounts>();
var msMap = new ConcurrentDictionary<string, DirectoryCounts>();
var worker = new MultiDriveCoordinatorWorker(gScanner, mScanner, pairs, googleMap, msMap, logger);
await worker.StartAsync();
```

`MultiDriveCoordinatorWorker` now uses `DirectoryScanner` under the hood so each
directory in both drives is counted individually. The maps above will contain an
entry for every nested path found beneath the provided roots.

## Command Line Interface

Run the CLI from the repository root:


```bash
dotnet run --project MetricsCli -- \
  --ms-root <drive-id> --google-root <folder-id> \
  --google-auth creds.json --output mismatches.csv \
  --max-dop 4 --follow-shortcuts
```

`PipelineRunner` now relies on `DirectoryScanner` so nested folder counts are
included in the CSV export.

### Options
* `--ms-root` – Microsoft Graph path or ID to scan.
* `--google-root` – Google Drive folder to compare.
* `--google-auth` – path to OAuth credentials JSON.
* `--output` – CSV file for mismatch results.
* `--max-dop` – maximum concurrency for API calls.
* `--follow-shortcuts` – resolve folder shortcuts in Google Drive.

When this flag is enabled the scanner treats Drive shortcuts to folders as real
directories. The credentials path can also be provided via the `GOOGLE_AUTH`
environment variable if `--google-auth` is omitted.

```bash
docker build -t metrics .
docker run --rm \
  -e AZURE_CLIENT_ID=$AZURE_CLIENT_ID \
  -e AZURE_TENANT_ID=$AZURE_TENANT_ID \
  -e AZURE_CLIENT_SECRET=$AZURE_CLIENT_SECRET \
  -e GOOGLE_AUTH=/secrets/creds.json \
  metrics --ms-root <drive-id> --google-root <folder-id>
```
The container entrypoint defaults to the CLI. You can mount an appsettings file to avoid passing long command arguments:
```bash
docker run --rm -v $(pwd)/appsettings.json:/app/appsettings.json \
  -e AZURE_CLIENT_ID -e AZURE_TENANT_ID -e AZURE_CLIENT_SECRET \
  metrics
```

Ensure your credentials file is mounted or baked into the container image. When
running in Docker set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` to suppress
locale warnings.

## Testing

Run the full test suite including coverage collection:

```bash
dotnet test --no-build --no-restore --collect:"XPlat Code Coverage"
```

Coverage reports are written to `MetricsPipeline.Core.Tests/TestResults` in
`coverage.cobertura.xml`. Use `reportgenerator` or a similar tool to produce an
HTML summary. Aim for coverage above 80% to catch regressions.

The BDD suite now includes a scenario checking shortcut resolution when
`--follow-shortcuts` is supplied.

Additional notes:
1. `DirectoryComparer` now works with `DirectoryEntry` results so scanners can provide IDs and names separately.
2. Unit tests may use a simple `FileSystemDriveScanner` stub to expose local folders as `DirectoryEntry` objects.
3. `DirectoryScanner` requires its dependencies via the constructor, making it easy to inject mocks during testing.
4. Run `dotnet build` before testing if feature files change so the generated bindings stay in sync.
5. Use `reportgenerator` to convert the Cobertura file to HTML and confirm coverage visually.
6. The scanner's concurrency can be tuned by passing a different `maxConcurrency` when constructing `DirectoryScanner`.
7. Worker classes now have dedicated unit tests. `DirectoryComparerWorker` and `DriveScannerWorker` are invoked via reflection so their protected `ExecuteAsync` methods run without a host.
8. New tests cover `GetCountsAsync` for both scanner implementations ensuring file and directory totals are tallied correctly.
9. `GraphScanner` and `GoogleDriveScanner` are excluded from coverage because they rely on live cloud APIs.
10. Execute `dotnet test IdealComputingMachine.sln` to build and run all projects together.


