using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Security;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Text;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using SharedDrive = Google.Apis.Drive.v3.Data.Drive;

namespace Caf.Midden.Cli.Services;

public sealed class GoogleWorkspaceSharedDriveCrawler : ICrawl
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveReadonly];

    private readonly DriveService service;
    private readonly ICrawlLogger logger;
    private List<SharedDrive>? cachedDriveList;

    public GoogleWorkspaceSharedDriveCrawler(
        string clientId,
        string clientSecret,
        string applicationName,
        string? tokenStorePath = null,
        ICrawlLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        var credential = GoogleCredentialFactory.Authorize(clientId, clientSecret, tokenStorePath);

        service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });

        GoogleDriveServiceFactory.ConfigureRetry(service.HttpClient);
        this.logger = logger ?? ConsoleCrawlLogger.Instance;
    }

    public GoogleWorkspaceSharedDriveCrawler(string jsonKeyPath, string applicationName, ICrawlLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonKeyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        var credential = GoogleCredentialFactory.FromServiceAccountFile(jsonKeyPath);

        service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });

        GoogleDriveServiceFactory.ConfigureRetry(service.HttpClient);
        this.logger = logger ?? ConsoleCrawlLogger.Instance;
    }

    public void Dispose()
    {
        service.Dispose();
    }

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

    private List<DriveFile> GetFiles(
        string fileNameContains = MiddenFileConventions.MiddenFileExtension,
        bool fileNameContainsIsExactMatch = false,
        string? fileNameEndsWith = null)
    {
        List<DriveFile> files = [];

        foreach (var drive in GetSharedDrives())
        {
            string? pageToken = null;

            do
            {
                var listRequest = service.Files.List();
                listRequest.DriveId = drive.Id;
                listRequest.PageSize = 100;
                listRequest.Fields = "nextPageToken, files(id, name, parents, driveId, trashed)";
                listRequest.IncludeItemsFromAllDrives = true;
                listRequest.SupportsAllDrives = true;
                listRequest.Corpora = "drive";
                listRequest.PageToken = pageToken;
                listRequest.Q = fileNameContainsIsExactMatch
                    ? $"name = '{GoogleDriveQuery.EscapeTerm(fileNameContains)}'"
                    : $"name contains '{GoogleDriveQuery.EscapeTerm(fileNameContains)}'";

                var response = listRequest.Execute();
                var driveFiles = response.Files ?? [];

                foreach (var file in driveFiles)
                {
                    if (file.Trashed == true)
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

    private List<SharedDrive> GetSharedDrives()
    {
        cachedDriveList ??= service.Drives.List().Execute().Drives?.ToList() ?? [];
        return cachedDriveList;
    }

    private string DownloadFileText(string fileId)
    {
        using var memoryStream = new MemoryStream();
        var fileRequest = service.Files.Get(fileId);
        fileRequest.SupportsAllDrives = true;
        fileRequest.Download(memoryStream);
        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private string GetAbsolutePath(DriveFile file)
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

    private DriveFile GetFile(string id)
    {
        var request = service.Files.Get(id);
        request.Fields = "id, name, parents, driveId, trashed";
        request.SupportsAllDrives = true;
        return request.Execute();
    }
}
