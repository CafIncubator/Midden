using Caf.Midden.Cli.Services;
using System.Text;

namespace Caf.Midden.Cli.Tests;

internal sealed class FakeAzureDataLakeGateway : IAzureDataLakeGateway
{
    private readonly List<AzureDataLakeItem> paths = [];
    private readonly Dictionary<string, string> contents = [];

    public int ListPathsCallCount { get; private set; }

    public FakeAzureDataLakeGateway AddFile(string path, string content)
    {
        paths.Add(new AzureDataLakeItem(path, IsDirectory: false));
        contents[path] = content;
        return this;
    }

    public FakeAzureDataLakeGateway AddDirectory(string path)
    {
        paths.Add(new AzureDataLakeItem(path, IsDirectory: true));
        return this;
    }

    public IReadOnlyList<AzureDataLakeItem> ListPaths()
    {
        ListPathsCallCount++;
        return paths;
    }

    public Stream OpenRead(string path) => new MemoryStream(Encoding.UTF8.GetBytes(contents[path]));

    public void Dispose()
    {
    }
}