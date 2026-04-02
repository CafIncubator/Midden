namespace Caf.Midden.Cli.Models;

public sealed class DataStore
{
    public string Name { get; init; } = string.Empty;
    public DataStoreTypes Type { get; init; }
    public string? Path { get; init; }
    public string? TenantId { get; init; }
    public string? AccountName { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? AzureFileSystemName { get; init; }
    public string? ApplicationName { get; init; }
    public string? SharedAccessSignature { get; init; }
    public string? Uri { get; init; }
    public bool ShouldCollateProjects { get; init; }

    // Used for authentication methods that can be configured using a file, like a json file.
    public string? AuthFilePath { get; init; }
}
