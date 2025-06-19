# Ideal Computing Machine

## Overview

Ideal Computing Machine demonstrates a minimal background worker and command line
tool built with **.NET&nbsp;9**. `DirectorySyncWorker` coordinates directory
scans across Microsoft and Google drives using reusable helpers from the
`MetricsPipeline.Core` library. The library provides:

* `GraphScanner` for enumerating OneDrive or SharePoint locations via Microsoft
  Graph.
* `GoogleDriveScanner` for traversing Google Drive. It can optionally resolve
  shortcut targets when `--follow-shortcuts` is enabled.
* `MultiDriveCoordinatorWorker` to aggregate counts from both platforms in
  parallel.
* `DirectoryCountsComparer` and `CsvExporter` to report mismatched folder
  statistics.

Scanning is recursive by default so nested folders are processed automatically.
When `--follow-shortcuts` is specified, Google Drive shortcut items pointing to
folders are also descended into.

## Setup

1. Install the .NET&nbsp;9 SDK (use `dotnet-install.sh` or
   [download](https://aka.ms/dotnet-download)).
2. Run `dotnet restore` to fetch dependencies.
3. Build the solution with `dotnet build`.
4. Disable telemetry if desired: `export DOTNET_CLI_TELEMETRY_OPTOUT=1`.
5. Execute `dotnet run --project DirectorySyncWorker` to launch the sample
   worker service.

## OAuth configuration

### Register a Microsoft application

1. Sign in to the [Azure portal](https://portal.azure.com/) and open
   **Azure&nbsp;Active Directory**.
2. Choose **App registrations** &rarr; **New registration** and create an app.
3. Under **API permissions** add `Files.Read.All` and `Sites.Read.All` as
   application permissions and grant admin consent.
4. In **Certificates &amp; secrets** create a client secret.
5. Note the **Application (client) ID** and **Directory (tenant) ID** and set
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_CLIENT_SECRET` in your shell.

### Register a Google application

1. Visit the [Google Cloud Console](https://console.cloud.google.com/)
   and create a new project.
2. Enable the **Google Drive API** for the project.
3. Create a **Service account** and download its key as JSON.
4. Share the target Google Drive folder with that service account.
5. Provide the JSON path via `--google-auth` or set the `GOOGLE_AUTH`
   environment variable.

## Running the CLI

Invoke the CLI from the repository root:

```bash
dotnet run --project MetricsCli -- \
  --ms-root <drive-id> --google-root <folder-id> \
  --google-auth creds.json --output mismatches.csv \
  --follow-shortcuts --max-dop 4
```

**Options**

* `--ms-root` – Microsoft Graph path or drive ID to scan.
* `--google-root` – Google Drive folder to compare.
* `--google-auth` – path to OAuth credentials JSON.
* `--output` – CSV file for mismatch results.
* `--max-dop` – maximum concurrency for API calls.
* `--follow-shortcuts` – recursively resolve Google shortcuts that point to
  folders.

## Docker usage

Container images can be built for automated deployments:

```bash
docker build -t metrics .
docker run --rm \
  -e AZURE_CLIENT_ID=$AZURE_CLIENT_ID \
  -e AZURE_TENANT_ID=$AZURE_TENANT_ID \
  -e AZURE_CLIENT_SECRET=$AZURE_CLIENT_SECRET \
  -e GOOGLE_AUTH=/secrets/creds.json \
  metrics --ms-root <drive-id> --google-root <folder-id>
```

Ensure your credentials file is mounted or baked into the container image. When
running in Docker set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` to suppress
locale warnings.

## Testing

Run the full test suite including coverage collection:

```bash
dotnet test --no-build --no-restore --collect:"XPlat Code Coverage"
```

CI executions use the same command and expect coverage above 80%. Coverage
reports are written to `MetricsPipeline.Core.Tests/TestResults` as
`coverage.cobertura.xml` and can be converted to HTML with tools like
`reportgenerator`.

