using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System.Text;

namespace Caf.Midden.Cli.Services;

public sealed class AzureDataLakeCrawler : ICrawl
{
    private const string MiddenFileExtension = ".midden";
    private const string MippenFileSearchTerm = "DESCRIPTION.md";

    private readonly DataLakeFileSystemClient fileSystemClient;

    public AzureDataLakeCrawler(
        string accountName,
        string tenantId,
        string clientId,
        string clientSecret,
        string fileSystemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileSystemName);

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret, new TokenCredentialOptions());
        var serviceClient = new DataLakeServiceClient(new Uri($"https://{accountName}.dfs.core.windows.net"), credential);
        fileSystemClient = serviceClient.GetFileSystemClient(fileSystemName);
    }

    public IReadOnlyList<string> GetFileNames(string fileExtension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        List<string> names = [];

        foreach (var pathItem in fileSystemClient.GetPaths())
        {
            if (pathItem.IsDirectory != true)
            {
                continue;
            }

            foreach (var subPathItem in fileSystemClient.GetPaths(pathItem.Name, false, false, CancellationToken.None))
            {
                if (!subPathItem.Name.Contains(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.WriteLine($"  In {pathItem.Name} found {subPathItem.Name}");
                names.Add(subPathItem.Name);
            }
        }

        Console.WriteLine($"Found a total of {names.Count} files");
        return names;
    }

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser)
    {
        List<Metadata> metadatas = [];

        foreach (var fileName in GetFileNames(MiddenFileExtension))
        {
            try
            {
                var fileClient = fileSystemClient.GetFileClient(fileName);
                using var stream = fileClient.OpenRead();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);

                var json = Encoding.UTF8.GetString(memoryStream.ToArray());
                var metadata = parser.Parse(json);
                metadata.Dataset.DatasetPath = fileClient.Path.Replace(MiddenFileExtension, string.Empty, StringComparison.OrdinalIgnoreCase);
                metadatas.Add(metadata);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Error parsing file '{fileName}': {exception.Message}");
            }
        }

        return metadatas;
    }

    public IReadOnlyList<Project> GetProjects(ProjectReader reader)
    {
        List<Project> projects = [];

        foreach (var fileName in GetFileNames(MippenFileSearchTerm))
        {
            var fileClient = fileSystemClient.GetFileClient(fileName);
            using var stream = fileClient.OpenRead();
            var project = reader.Read(stream);

            if (project is not null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }
}
