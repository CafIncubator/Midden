namespace Caf.Midden.Cli.Services;

internal sealed record AzureDataLakeItem(string Name, bool IsDirectory);

internal interface IAzureDataLakeGateway : IDisposable
{
    IReadOnlyList<AzureDataLakeItem> ListPaths();
    Stream OpenRead(string path);
}