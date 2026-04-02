using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Text;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Caf.Midden.Cli.Services;

public sealed class GoogleDriveCrawler : ICrawl
{
    private const string MiddenFileExtension = ".midden";
    private const string MippenFileSearchTerm = "DESCRIPTION.md";

    private static readonly string[] Scopes = [DriveService.Scope.DriveReadonly];

    private readonly DriveService service;

    public GoogleDriveCrawler(string clientId, string clientSecret, string applicationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        var clientSecrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
        };

        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("token.json", true))
            .GetAwaiter()
            .GetResult();

        service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });
    }

    public IReadOnlyList<string> GetFileNames(string fileNameContains)
    {
        var names = GetFiles(fileNameContains, false, fileNameContains)
            .Select(file => file.Id)
            .ToList();

        Console.WriteLine($"Found a total of {names.Count} files");
        return names;
    }

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser)
    {
        List<Metadata> metadatas = [];

        foreach (var file in GetFiles(MiddenFileExtension, false, MiddenFileExtension))
        {
            try
            {
                var metadata = parser.Parse(DownloadFileText(file.Id));
                metadata.Dataset.DatasetPath = GetAbsolutePath(file)
                    .Replace(MiddenFileExtension, string.Empty, StringComparison.OrdinalIgnoreCase);
                metadatas.Add(metadata);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Skipping file '{file.Name}': {exception.Message}");
            }
        }

        return metadatas;
    }

    public IReadOnlyList<Project> GetProjects(ProjectReader reader)
    {
        List<Project> projects = [];

        foreach (var file in GetFiles(MippenFileSearchTerm, true, ".md"))
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
        string fileNameContains = MiddenFileExtension,
        bool fileNameContainsIsExactMatch = false,
        string? fileNameEndsWith = null)
    {
        var listRequest = service.Files.List();
        listRequest.PageSize = 100;
        listRequest.Fields = "nextPageToken, files(id, name, parents, driveId, trashed)";
        listRequest.Q = fileNameContainsIsExactMatch
            ? $"name = '{fileNameContains}'"
            : $"name contains '{fileNameContains}'";

        var driveFiles = listRequest.Execute().Files ?? [];

        return driveFiles
            .Where(file => file.Trashed != true)
            .Where(file => string.IsNullOrWhiteSpace(fileNameEndsWith)
                || file.Name.EndsWith(fileNameEndsWith, StringComparison.OrdinalIgnoreCase))
            .Select(file =>
            {
                Console.WriteLine($"  Found {file.Name}");
                return file;
            })
            .ToList();
    }

    private string DownloadFileText(string fileId)
    {
        using var memoryStream = new MemoryStream();
        var fileRequest = service.Files.Get(fileId);
        fileRequest.SupportsAllDrives = true;
        fileRequest.Download(memoryStream);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
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
