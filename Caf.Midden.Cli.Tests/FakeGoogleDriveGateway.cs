using Caf.Midden.Cli.Services;

namespace Caf.Midden.Cli.Tests;

internal sealed class FakeGoogleDriveGateway : IGoogleDriveGateway
{
    private readonly Queue<GoogleDrivePage> pages = new();
    private readonly Dictionary<string, GoogleDriveItem> files = [];
    private readonly Dictionary<string, string> contents = [];
    private readonly List<GoogleSharedDrive> sharedDrives = [];

    public List<(string Query, string? PageToken, string? DriveId)> ListRequests { get; } = [];

    public FakeGoogleDriveGateway AddPage(GoogleDrivePage page)
    {
        pages.Enqueue(page);
        return this;
    }

    public FakeGoogleDriveGateway AddFile(GoogleDriveItem file)
    {
        files[file.Id] = file;
        return this;
    }

    public FakeGoogleDriveGateway AddContent(string id, string content)
    {
        contents[id] = content;
        return this;
    }

    public FakeGoogleDriveGateway AddSharedDrive(string id, string name)
    {
        sharedDrives.Add(new GoogleSharedDrive(id, name));
        return this;
    }

    public GoogleDrivePage ListFiles(string query, string? pageToken, string? driveId = null)
    {
        ListRequests.Add((query, pageToken, driveId));
        return pages.Dequeue();
    }

    public IReadOnlyList<GoogleSharedDrive> ListSharedDrives() => sharedDrives;

    public GoogleDriveItem GetFile(string id) => files[id];

    public string DownloadFileText(string id) => contents[id];

    public void Dispose()
    {
    }
}