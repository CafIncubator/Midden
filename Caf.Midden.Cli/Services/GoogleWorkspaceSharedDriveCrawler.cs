using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System.Text;

namespace Caf.Midden.Cli.Services;

public sealed class GoogleWorkspaceSharedDriveCrawler : ICrawl
{
    private readonly IGoogleDriveGateway gateway;
    private readonly ICrawlLogger logger;
    private IReadOnlyList<GoogleSharedDrive>? cachedDriveList;

    public GoogleWorkspaceSharedDriveCrawler(
        string clientId,
        string clientSecret,
        string applicationName,
        string? tokenStorePath = null,
        ICrawlLogger? logger = null)
        : this(
            GoogleDriveGateway.CreateWithOAuth(clientId, clientSecret, applicationName, tokenStorePath),
            logger)
    {
    }

    public GoogleWorkspaceSharedDriveCrawler(string jsonKeyPath, string applicationName, ICrawlLogger? logger = null)
        : this(GoogleDriveGateway.CreateWithServiceAccount(jsonKeyPath, applicationName), logger)
    {
    }

    internal GoogleWorkspaceSharedDriveCrawler(IGoogleDriveGateway gateway, ICrawlLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        this.gateway = gateway;
        this.logger = logger ?? ConsoleCrawlLogger.Instance;
    }

    public void Dispose() => gateway.Dispose();

    internal IReadOnlyList<string> GetFileNames(string fileNameContains)
    {
        var names = GetFiles(fileNameContains, false, fileNameContains)
            .Select(file => file.Id)
            .ToList();

        logger.Info($"Found a total of {names.Count} files");
        return names;
    }

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser)
    {
        List<Metadata> metadatas = [];

        foreach (var file in GetFiles(MiddenFileConventions.MiddenFileExtension, false, MiddenFileConventions.MiddenFileExtension))
        {
            try
            {
                var metadata = parser.Parse(DownloadFileText(file.Id));

                if (metadata.Dataset is null)
                {
                    logger.Warning($"Skipping shared drive file '{file.Name}': the file has no 'Dataset' section.");
                    continue;
                }

                metadata.Dataset.DatasetPath = MiddenFileConventions.TrimSuffix(GetAbsolutePath(file), MiddenFileConventions.MiddenFileExtension);
                metadatas.Add(metadata);
            }
            catch (Exception exception)
            {
                logger.Warning($"Skipping shared drive file '{file.Name}': {exception.Message}");
            }
        }

        return metadatas;
    }

    public IReadOnlyList<Project> GetProjects(ProjectReader reader)
    {
        List<Project> projects = [];

        foreach (var file in GetFiles(MiddenFileConventions.MippenFileSearchTerm, true, ".md"))
        {
            var fileString = DownloadFileText(file.Id);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileString));
            var project = reader.Read(stream);

            if (project is not null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }

    private List<GoogleDriveItem> GetFiles(
        string fileNameContains = MiddenFileConventions.MiddenFileExtension,
        bool fileNameContainsIsExactMatch = false,
        string? fileNameEndsWith = null)
    {
        List<GoogleDriveItem> files = [];

        foreach (var drive in GetSharedDrives())
        {
            string? pageToken = null;

            do
            {
                var query = fileNameContainsIsExactMatch
                    ? $"name = '{GoogleDriveQuery.EscapeTerm(fileNameContains)}'"
                    : $"name contains '{GoogleDriveQuery.EscapeTerm(fileNameContains)}'";
                var response = gateway.ListFiles(query, pageToken, drive.Id);

                foreach (var file in response.Files)
                {
                    if (file.IsTrashed)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(fileNameEndsWith)
                        && !file.Name.EndsWith(fileNameEndsWith, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    logger.Info($"  In {drive.Name} found {file.Name}");
                    files.Add(file);
                }

                pageToken = response.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));
        }

        return files;
    }

    private IReadOnlyList<GoogleSharedDrive> GetSharedDrives()
    {
        cachedDriveList ??= gateway.ListSharedDrives();
        return cachedDriveList;
    }

    private string DownloadFileText(string fileId) => gateway.DownloadFileText(fileId);

    private string GetAbsolutePath(GoogleDriveItem file)
    {
        if (file.Parents is not { Count: > 0 })
        {
            return file.Name;
        }

        var path = new List<string>();
        var currentFile = file;

        while (currentFile.Parents is { Count: > 0 })
        {
            var parent = GetFile(currentFile.Parents[0]);

            if (parent.Parents is null || parent.Parents.Count == 0)
            {
                var driveName = GetSharedDrives()
                    .FirstOrDefault(drive => drive.Id == parent.DriveId)
                    ?.Name;

                if (!string.IsNullOrWhiteSpace(driveName))
                {
                    path.Insert(0, driveName);
                }

                break;
            }

            path.Insert(0, parent.Name);
            currentFile = parent;
        }

        path.Add(file.Name);
        return path.Aggregate(Path.Combine);
    }

    private GoogleDriveItem GetFile(string id) => gateway.GetFile(id);
}
