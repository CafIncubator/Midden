using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System.Text;

namespace Caf.Midden.Cli.Services;

public sealed class AzureFileShareCrawler : ICrawl
{
    private const string MiddenFileExtension = ".midden";
    private const string MippenFileSearchTerm = "DESCRIPTION.md";

    private readonly string path;
    private readonly ShareClient shareClient;

    public AzureFileShareCrawler(string uri, string path, string sharedAccessSignature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedAccessSignature);

        this.path = path;
        shareClient = new ShareClient(new Uri(uri), new AzureSasCredential(sharedAccessSignature));
    }

    public IReadOnlyList<string> GetFileNames(string fileExtension)
    {
        List<string> names = [];

        try
        {
            foreach (var (directory, item) in EnumerateFiles())
            {
                if (!item.Name.Contains(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.WriteLine($"  In {directory.Name} found {item.Name}");
                names.Add(item.Name);
            }

            Console.WriteLine($"Found a total of {names.Count} files");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"An error occurred while listing files: {exception.Message}");
        }

        return names;
    }

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser)
    {
        List<Metadata> metadatas = [];

        try
        {
            foreach (var (directory, item) in EnumerateFiles())
            {
                if (!item.Name.Contains(MiddenFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.WriteLine($"  In {directory.Uri.AbsolutePath} found {item.Name}");
                var file = directory.GetFileClient(item.Name);
                var fileContents = file.Download();

                string json;
                using (var memoryStream = new MemoryStream())
                {
                    fileContents.Value.Content.CopyTo(memoryStream);
                    json = Encoding.UTF8.GetString(memoryStream.ToArray());
                }

                var metadata = parser.Parse(json);
                metadata.Dataset.DatasetPath = Path.GetRelativePath(path, file.Path)
                    .Replace(MiddenFileExtension, string.Empty, StringComparison.OrdinalIgnoreCase);
                metadatas.Add(metadata);
            }

            Console.WriteLine($"Found a total of {metadatas.Count} files");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"An error occurred while reading metadata: {exception.Message}");
        }

        return metadatas;
    }

    public IReadOnlyList<Project> GetProjects(ProjectReader reader)
    {
        List<Project> projects = [];

        try
        {
            foreach (var (directory, item) in EnumerateFiles())
            {
                if (!item.Name.Contains(MippenFileSearchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.WriteLine($"  In {directory.Uri.AbsolutePath} found {item.Name}");
                var file = directory.GetFileClient(item.Name);
                var fileContents = file.Download();

                using var stream = fileContents.Value.Content;
                var project = reader.Read(stream);

                if (project is not null)
                {
                    projects.Add(project);
                }
            }

            Console.WriteLine($"Found a total of {projects.Count} files");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"An error occurred while reading projects: {exception.Message}");
        }

        return projects;
    }

    private IEnumerable<(ShareDirectoryClient Directory, ShareFileItem Item)> EnumerateFiles()
    {
        var remaining = new Queue<ShareDirectoryClient>();
        remaining.Enqueue(shareClient.GetDirectoryClient(path));

        while (remaining.Count > 0)
        {
            var directory = remaining.Dequeue();

            foreach (var item in directory.GetFilesAndDirectories())
            {
                if (item.IsDirectory)
                {
                    remaining.Enqueue(directory.GetSubdirectoryClient(item.Name));
                    continue;
                }

                yield return (directory, item);
            }
        }
    }
}
