# Ideal Computing Machine

This repository demonstrates a minimal setup for a worker service using **.NET 9**.
The service, `DirectorySyncWorker`, processes background jobs that keep directories in sync across environments.

## Prerequisites
- .NET 9 SDK (install via `dotnet-install.sh` or from the official [download page](https://aka.ms/dotnet-download))
- A Unix-like shell capable of running bash scripts
- Git for version control

## Usage
1. Restore dependencies with `dotnet restore`.
2. Build the solution using `dotnet build`.
3. Run the worker with `dotnet run --project DirectorySyncWorker`.
4. Execute tests and generate coverage: `dotnet test --collect:"XPlat Code Coverage"`.
5. Disable telemetry during builds by setting `DOTNET_CLI_TELEMETRY_OPTOUT=1`.

This solution serves as a starting point for building background services.
Refer to the [.NET 9 release notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9) for new features.
Feel free to extend it with your own business logic and tests.
