using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Security;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Text;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Caf.Midden.Cli.Services;

internal sealed class GoogleDriveGateway : IGoogleDriveGateway
{
    private readonly DriveService service;

    private GoogleDriveGateway(DriveService service)
    {
        this.service = service;
        GoogleDriveServiceFactory.ConfigureRetry(service.HttpClient);
    }

    public static GoogleDriveGateway CreateWithOAuth(
        string clientId,
        string clientSecret,
        string applicationName,
        string? tokenStorePath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        var credential = GoogleCredentialFactory.Authorize(clientId, clientSecret, tokenStorePath);
        var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });

        return new GoogleDriveGateway(service);
    }

    public static GoogleDriveGateway CreateWithServiceAccount(string jsonKeyPath, string applicationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonKeyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        var credential = GoogleCredentialFactory.FromServiceAccountFile(jsonKeyPath);
        var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });

        return new GoogleDriveGateway(service);
    }

    public GoogleDrivePage ListFiles(string query, string? pageToken, string? driveId = null)
    {
        var request = service.Files.List();
        request.PageSize = 100;
        request.Fields = "nextPageToken, files(id, name, parents, driveId, trashed)";
        request.SupportsAllDrives = true;
        request.IncludeItemsFromAllDrives = true;
        request.PageToken = pageToken;
        request.Q = query;

        if (!string.IsNullOrWhiteSpace(driveId))
        {
            request.DriveId = driveId;
            request.Corpora = "drive";
        }

        var response = request.Execute();
        var files = (response.Files ?? [])
            .Select(ToItem)
            .ToList();

        return new GoogleDrivePage(files, response.NextPageToken);
    }

    public IReadOnlyList<GoogleSharedDrive> ListSharedDrives() =>
        (service.Drives.List().Execute().Drives ?? [])
            .Select(drive => new GoogleSharedDrive(drive.Id, drive.Name))
            .ToList();

    public GoogleDriveItem GetFile(string id)
    {
        var request = service.Files.Get(id);
        request.Fields = "id, name, parents, driveId, trashed";
        request.SupportsAllDrives = true;
        return ToItem(request.Execute());
    }

    public string DownloadFileText(string id)
    {
        using var memoryStream = new MemoryStream();
        var request = service.Files.Get(id);
        request.SupportsAllDrives = true;
        request.Download(memoryStream);
        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public void Dispose() => service.Dispose();

    private static GoogleDriveItem ToItem(DriveFile file) =>
        new(file.Id, file.Name, file.Parents?.ToList() ?? [], file.DriveId, file.Trashed == true);
}