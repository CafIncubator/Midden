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
    private readonly string path;
    private readonly ShareClient shareClient;
    private readonly ICrawlLogger logger;
    private List<(ShareDirectoryClient Directory, ShareFileItem Item)>? cachedFiles;

    public AzureFileShareCrawler(string uri, string path, string sharedAccessSignature, ICrawlLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedAccessSignature);

        this.path = path;
        shareClient = new ShareClient(new Uri(uri), new AzureSasCredential(sharedAccessSignature));
        this.logger = logger ?? ConsoleCrawlLogger.Instance;
    }

    public void Dispose()
    {
        // ShareClient holds no unmanaged resources requiring explicit disposal.
    }

    internal IReadOnlyList<string> GetFileNames(string fileExtension)
    {
        List<string> names = [];

        try
        {
            foreach (var (directory, item) in EnumerateFiles())
            {
                if (!item.Name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                logger.Info($"  In {directory.Name} found {item.Name}");
                names.Add(item.Name);
            }

            logger.Info($"Found a total of {names.Count} files");
        }
        catch (Exception exception)
        {
            logger.Warning($"An error occurred while listing files: {exception.Message}");
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
                if (!item.Name.EndsWith(MiddenFileConventions.MiddenFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                logger.Info($"  In {directory.Uri.AbsolutePath} found {item.Name}");
                var file = directory.GetFileClient(item.Name);
                var fileContents = file.Download();

                string json;
                using (var stream = fileContents.Value.Content)
                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    json = reader.ReadToEnd();
                }

                var metadata = parser.Parse(json);

                if (metadata.Dataset is null)
                {
                    logger.Warning($"Skipping file '{item.Name}': the file has no 'Dataset' section.");
                    continue;
                }

                metadata.Dataset.DatasetPath = MiddenFileConventions.TrimSuffix(
                    Path.GetRelativePath(path, file.Path),
                    MiddenFileConventions.MiddenFileExtension);
                metadatas.Add(metadata);
            }

            logger.Info($"Found a total of {metadatas.Count} files");
        }
        catch (Exception exception)
        {
            logger.Warning($"An error occurred while reading metadata: {exception.Message}");
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
                if (!item.Name.EndsWith(MiddenFileConventions.MippenFileSearchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                logger.Info($"  In {directory.Uri.AbsolutePath} found {item.Name}");
                var file = directory.GetFileClient(item.Name);
                var fileContents = file.Download();

                using var stream = fileContents.Value.Content;
                var project = reader.Read(stream);

                if (project is not null)
                {
                    projects.Add(project);
                }
            }

            logger.Info($"Found a total of {projects.Count} files");
        }
        catch (Exception exception)
        {
            logger.Warning($"An error occurred while reading projects: {exception.Message}");
        }

        return projects;
    }

    private IEnumerable<(ShareDirectoryClient Directory, ShareFileItem Item)> EnumerateFiles()
    {
        if (cachedFiles is not null)
        {
            return cachedFiles;
        }

        cachedFiles = [];
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

                cachedFiles.Add((directory, item));
            }
        }

        return cachedFiles;
    }
}
