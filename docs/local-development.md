# Local Development

This project uses Azure Table Storage as the default persistence layer in all environments, including local development.

For local development, we standardize on [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite), which emulates Azure Storage locally so you can run the real persistence path without needing an Azure subscription or live storage account.

## Prerequisites

- Docker Desktop or another Docker-compatible runtime with `docker compose`
- .NET SDK

## Start Azurite

From the repository root, start the checked-in Azurite service:

```powershell
docker compose -f compose.azurite.yml up -d
```

On macOS or Linux:

```bash
docker compose -f compose.azurite.yml up -d
```

Azurite exposes the default local Azure Storage ports:

- Blob: `10000`
- Queue: `10001`
- Table: `10002`

The emulator data is stored in `.azurite/`, which is git-ignored.

## Local Function Settings

The Functions app is already configured to use Azurite by default through:

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true"
  }
}
```

That means the default repository path stays on Azure Table Storage locally, with no extra connection string needed as long as Azurite is running on its standard ports.

## Run the App

With Azurite running, you can start the solution normally. The backend will create the configured redirects table automatically on first use.

The local web UI is served from:

```text
/ui
```

The root path `/` redirects to `/ui`, while alias lookups still use `/{alias}`.

If you want to use the Functions app directly:

```powershell
cd src/UrlRedirect.Functions
func start
```

On macOS or Linux:

```bash
cd src/UrlRedirect.Functions
func start
```

## Run Integration Tests

The Azure Table integration tests use Azurite as well, so keep the emulator running and then execute:

```powershell
dotnet test src/UrlRedirect.sln /p:RestoreIgnoreFailedSources=true
```

On macOS or Linux:

```bash
dotnet test src/UrlRedirect.sln /p:RestoreIgnoreFailedSources=true
```

If Azurite is not running, the integration test class skips itself with a message telling you how to start the emulator.

## Stop Azurite

```powershell
docker compose -f compose.azurite.yml down
```

On macOS or Linux:

```bash
docker compose -f compose.azurite.yml down
```
