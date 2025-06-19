# Ideal Computing Machine

This repository demonstrates a minimal setup for a worker service using **.NET 9**.
The service, `DirectorySyncWorker`, processes background jobs that keep directories in sync across environments.
It now includes a reusable library called `MetricsPipeline.Core` that provides drive
scanning and directory comparison helpers.
A new `GraphScanner` leverages the Microsoft Graph SDK to enumerate OneDrive or
SharePoint document libraries. It automatically handles throttling and parallel
requests.

## Prerequisites
- .NET 9 SDK (install via `dotnet-install.sh` or from the official [download page](https://aka.ms/dotnet-download))
- A Unix-like shell capable of running bash scripts
- Git for version control
- `Microsoft.Graph` NuGet package for Graph scanning features

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

### Graph Scanning Example
```csharp
var credential = new DefaultAzureCredential();
var graphClient = new GraphServiceClient(credential);
var scanner = new GraphScanner(graphClient, logger);
var folders = await scanner.GetDirectoriesAsync("{driveId}:{rootItemId}");
```

The scanner restricts concurrency with `SemaphoreSlim` and retries 429 responses
using Polly's `WaitAndRetryAsync` policy.

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
