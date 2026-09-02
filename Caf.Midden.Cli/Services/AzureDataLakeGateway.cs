using Azure.Storage.Files.DataLake;

namespace Caf.Midden.Cli.Services;

internal sealed class AzureDataLakeGateway(DataLakeFileSystemClient fileSystemClient) : IAzureDataLakeGateway
{
    public IReadOnlyList<AzureDataLakeItem> ListPaths() =>
        fileSystemClient
            .GetPaths(path: null, recursive: true, userPrincipalName: false, cancellationToken: CancellationToken.None)
            .Select(path => new AzureDataLakeItem(path.Name, path.IsDirectory == true))
            .ToList();

    public Stream OpenRead(string path) => fileSystemClient.GetFileClient(path).OpenRead();

    public void Dispose()
    {
    }
}