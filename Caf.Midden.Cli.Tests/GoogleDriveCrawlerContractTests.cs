using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Tests;

public class GoogleDriveCrawlerContractTests
{
    [Fact]
    public void GetMetadatas_MultiplePages_FiltersAndBuildsPaths()
    {
        var gateway = new FakeGoogleDriveGateway()
            .AddPage(new GoogleDrivePage(
                [
                    new("dataset-1", "DatasetOne.midden", ["folder"]),
                    new("trashed", "Trashed.midden", [], IsTrashed: true),
                    new("backup", "Backup.midden.bak", []),
                ],
                "page-2"))
            .AddPage(new GoogleDrivePage([new("dataset-2", "DatasetTwo.MIDDEN", [])]))
            .AddFile(new GoogleDriveItem("folder", "Raw", ["root"]))
            .AddFile(new GoogleDriveItem("root", "My Drive", []))
            .AddContent("dataset-1", MetadataJson("DatasetOne"))
            .AddContent("dataset-2", MetadataJson("DatasetTwo"));
        using var sut = new GoogleDriveCrawler(gateway);

        var actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        Assert.Equal(2, actual.Count);
        Assert.Equal("Raw\\DatasetOne", actual[0].Dataset.DatasetPath);
        Assert.Equal("DatasetTwo", actual[1].Dataset.DatasetPath);
        Assert.Collection(
            gateway.ListRequests,
            request =>
            {
                Assert.Equal("name contains '.midden'", request.Query);
                Assert.Null(request.PageToken);
                Assert.Null(request.DriveId);
            },
            request => Assert.Equal("page-2", request.PageToken));
    }

    [Fact]
    public void GetProjects_SharedDrives_QueriesEachDriveAndReturnsProjects()
    {
        var gateway = new FakeGoogleDriveGateway()
            .AddSharedDrive("drive-1", "Research")
            .AddSharedDrive("drive-2", "Archive")
            .AddPage(new GoogleDrivePage([new("project", "DESCRIPTION.md", [])]))
            .AddPage(new GoogleDrivePage([]))
            .AddContent("project", "---\r\nproject: \"ProductionProject\"\r\n---\r\n# Production\r\n");
        using var sut = new GoogleWorkspaceSharedDriveCrawler(gateway);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.Contains(actual, project => project.Name == "ProductionProject");
        Assert.Collection(
            gateway.ListRequests,
            request =>
            {
                Assert.Equal("name = 'DESCRIPTION.md'", request.Query);
                Assert.Equal("drive-1", request.DriveId);
            },
            request => Assert.Equal("drive-2", request.DriveId));
    }

    private static string MetadataJson(string name) => $$"""
        {
          "file": { "schema-version": "v0.1.0-alpha2", "creation-date": "2020-07-29" },
          "dataset": { "zone": "Raw", "project": "ContractTests", "name": "{{name}}" }
        }
        """;
}