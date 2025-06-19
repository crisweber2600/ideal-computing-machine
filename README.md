# Ideal Computing Machine

This repository demonstrates a minimal setup for a worker service using **.NET 9**.
The service, `DirectorySyncWorker`, processes background jobs that keep directories in sync across environments.
It now includes a reusable library called `MetricsPipeline.Core` that provides drive
scanning and directory comparison helpers.
A new `GraphScanner` leverages the Microsoft Graph SDK to enumerate OneDrive or
SharePoint document libraries. It automatically handles throttling and parallel
requests.
The library now also offers a `GoogleDriveScanner` for listing folders in
Google Drive. It shares the same concurrency limits and retry behaviour as the
Graph implementation and can optionally resolve shortcuts.

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
13. When running inside a minimal container you may set
    `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` to suppress locale warnings.

14. Use `DirectoryCountsComparer` to join Google and Microsoft maps and spot count mismatches.
15. `CsvExporter` streams these results directly to disk or stdout using `StreamWriter`.
16. A new feature file exercises the comparer so coverage remains high.
17. Example scripts now show how to pipe mismatches to a CSV file.
18. The README clarifies installing the .NET 9 preview SDK for this project.
19. The new `MetricsCli` tool runs the comparison pipeline from the command line.
20. Provide Microsoft and Google root IDs via `--ms-root` and `--google-root`.
21. Pass Google credentials with `--google-auth` or set the `GOOGLE_AUTH` environment variable.
22. Use `--output` to write mismatches to a CSV file.
23. Limit concurrency with the `--max-dop` option.
24. Step definitions now resolve services via Microsoft.Extensions.DependencyInjection.
25. `ScenarioDependencies` registers mocks for pipeline BDD tests.
26. A new feature checks that only mismatched entries reach the CSV export.
27. Moq supplies scanner stubs so tests remain fast and isolated.

28. Run `dotnet test --collect:"XPlat Code Coverage"` to verify coverage above 80%.
29. Configure OAuth credentials for Microsoft and Google before running scanners.
30. The CLI now supports environment variables for secret management.

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
The Google implementation also uses a semaphore to limit concurrent requests and
applies exponential back-off when the Drive API returns 429 or 503 errors.

This solution serves as a starting point for building background services.
Refer to the [.NET 9 release notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9) for new features.
Feel free to extend it with your own business logic and tests.

## Project Structure
- **DirectorySyncWorker** – executable worker service.
- **MetricsPipeline.Core** – contains interfaces like `IDriveScanner` and
  `IDirectoryComparer` plus model records for comparison results.
- Worker classes reside under `MetricsPipeline.Core/Infrastructure/Workers` so
  they can be shared across services.

### Directory Scanning Example
```csharp
var scanner = serviceProvider.GetRequiredService<IDriveScanner>();
var counts = await scanner.GetCountsAsync("/data");
Console.WriteLine($"Files: {counts.FileCount}, Dirs: {counts.DirectoryCount}");
```

### Coordinated Drive Example
```csharp
var pairs = new[]{("gRoot","mRoot")};
var googleMap = new ConcurrentDictionary<string, DirectoryCounts>();
var msMap = new ConcurrentDictionary<string, DirectoryCounts>();
var worker = new MultiDriveCoordinatorWorker(gScanner, mScanner, pairs, googleMap, msMap, logger);
await worker.StartAsync();
```

## Command Line Interface

Run the CLI from the repository root:

```bash
dotnet run --project MetricsCli -- \
  --ms-root <drive-id> --google-root <folder-id> \
  --google-auth creds.json --output mismatches.csv \
  --max-dop 4
```

### Options
* `--ms-root` – Microsoft Graph path or ID to scan.
* `--google-root` – Google Drive folder to compare.
* `--google-auth` – path to OAuth credentials JSON (defaults to `GOOGLE_AUTH`).
* `--output` – CSV file for mismatch results.
* `--max-dop` – maximum concurrency for API calls.

Currently the tool compares a single pair of roots and only counts folders and
files. It does not yet validate file content or sizes.

## Testing and Coverage

Execute the full test suite with coverage collection:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage reports are written to `MetricsPipeline.Core.Tests/TestResults` in
`coverage.cobertura.xml`. Use `reportgenerator` or a similar tool to produce an
HTML summary. Aim for coverage above 80% to catch regressions.
