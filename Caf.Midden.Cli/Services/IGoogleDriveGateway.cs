namespace Caf.Midden.Cli.Services;

internal sealed record GoogleDriveItem(
    string Id,
    string Name,
    IReadOnlyList<string> Parents,
    string? DriveId = null,
    bool IsTrashed = false);

internal sealed record GoogleDrivePage(IReadOnlyList<GoogleDriveItem> Files, string? NextPageToken = null);

internal sealed record GoogleSharedDrive(string Id, string Name);

internal interface IGoogleDriveGateway : IDisposable
{
    GoogleDrivePage ListFiles(string query, string? pageToken, string? driveId = null);
    IReadOnlyList<GoogleSharedDrive> ListSharedDrives();
    GoogleDriveItem GetFile(string id);
    string DownloadFileText(string id);
}