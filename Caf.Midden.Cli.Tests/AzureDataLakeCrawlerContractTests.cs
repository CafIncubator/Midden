using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Tests;

public class AzureDataLakeCrawlerContractTests
{
    [Fact]
    public void MetadataAndProjects_SharedListing_FiltersParsesAndCaches()
    {
        var gateway = new FakeAzureDataLakeGateway()
            .AddDirectory("ignored.midden")
            .AddFile("Raw/Dataset.midden", """
                {
                  "file": { "schema-version": "v0.1.0-alpha2", "creation-date": "2020-07-29" },
                  "dataset": { "zone": "Raw", "project": "ContractTests", "name": "Dataset" }
                }
                """)
            .AddFile("Raw/Backup.midden.bak", "not metadata")
            .AddFile(
                "Production/DESCRIPTION.md",
                "---\r\nproject: \"ProductionProject\"\r\n---\r\n# Production\r\n");
        using var sut = new AzureDataLakeCrawler(gateway);

        var metadatas = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));
        var projects = sut.GetProjects(new ProjectReader(new ProjectParser()));

        var metadata = Assert.Single(metadatas);
        Assert.Equal("Raw/Dataset", metadata.Dataset.DatasetPath);
        Assert.Contains(projects, project => project.Name == "ProductionProject");
        Assert.Equal(1, gateway.ListPathsCallCount);
    }
}