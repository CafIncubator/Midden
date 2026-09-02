using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.LiveTests;

[Collection(LiveCloudTestCollection.Name)]
[Trait("Category", "LiveIntegration")]
[Trait("Provider", "GoogleDrive")]
public class GoogleDriveLiveTests
{
    private const string ConfigPath = "Assets/CliConfigurationSecrets/GoogleDriveProjectTest.json";

    [Fact(Explicit = true)]
    public void GetFileNames_ReturnsResults()
    {
        var dataStore = LiveTestConfiguration.GetDataStoreOrSkip(ConfigPath);
        using var sut = new GoogleDriveCrawler(
            dataStore.ClientId!,
            dataStore.ClientSecret!,
            dataStore.ApplicationName!);

        var actual = sut.GetFileNames(".midden");

        Assert.NotNull(actual);
    }

    [Fact(Explicit = true)]
    public void GetMetadatas_ReturnsResults()
    {
        var dataStore = LiveTestConfiguration.GetDataStoreOrSkip(ConfigPath);
        using var sut = new GoogleDriveCrawler(
            dataStore.ClientId!,
            dataStore.ClientSecret!,
            dataStore.ApplicationName!);

        var actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        Assert.NotNull(actual);
    }

    [Fact(Explicit = true)]
    public void GetProjects_ReturnsProductionProject()
    {
        var dataStore = LiveTestConfiguration.GetDataStoreOrSkip(ConfigPath);
        using var sut = new GoogleDriveCrawler(
            dataStore.ClientId!,
            dataStore.ClientSecret!,
            dataStore.ApplicationName!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.Contains(actual, project => project.Name == "ProductionProject");
    }
}