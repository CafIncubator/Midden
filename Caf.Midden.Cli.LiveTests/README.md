# CLI live integration tests

This project contains smoke tests that connect to real Azure Data Lake and Google Drive
resources. The tests are compiled with the solution but use xUnit v3 explicit tests, so the
default `dotnet test Caf.Midden.slnx` command does not run them.

## Configure credentials

Place the configuration files needed by the providers you want to test under
`Caf.Midden.Cli.LiveTests/Assets/CliConfigurationSecrets/`:

- `AzureDataLakeProjectTest.json`
- `GoogleDriveProjectTest.json`
- `GoogleWorkspaceSharedDriveProjectTest.json`
- `GoogleWorkspaceSharedDriveProjectTestWithServiceAccount.json`

The service-account configuration must reference its JSON key through `AuthFilePath`. Keep that
key in the same ignored directory. The directory contents are git-ignored; never commit test
credentials, cached OAuth tokens, or service-account keys.

## Run live tests

From the repository root, run only the explicit tests:

```powershell
dotnet test Caf.Midden.Cli.LiveTests --configuration Release -- xUnit.Explicit=only
```

Filter by provider when diagnosing a specific integration:

```powershell
dotnet test Caf.Midden.Cli.LiveTests --configuration Release --filter "Provider=GoogleDrive" -- xUnit.Explicit=only
dotnet test Caf.Midden.Cli.LiveTests --configuration Release --filter "Provider=GoogleWorkspace" -- xUnit.Explicit=only
dotnet test Caf.Midden.Cli.LiveTests --configuration Release --filter "Provider=AzureDataLake" -- xUnit.Explicit=only
```

Missing configuration produces an xUnit skip with the missing path. A protected live-test CI job
should treat those skips as failures so a credential outage cannot look healthy:

```powershell
dotnet test Caf.Midden.Cli.LiveTests --configuration Release -- xUnit.Explicit=only xUnit.FailSkips=true
```

The live tests share a nonparallel collection because Google OAuth token state and remote fixture
data are shared resources.