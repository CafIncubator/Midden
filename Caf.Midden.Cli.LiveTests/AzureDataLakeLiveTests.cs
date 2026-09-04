using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;

namespace Caf.Midden.Cli.LiveTests;

[Collection(LiveCloudTestCollection.Name)]
[Trait("Category", "LiveIntegration")]
[Trait("Provider", "AzureDataLake")]
public class AzureDataLakeLiveTests
{
    private const string ConfigPath = "Assets/CliConfigurationSecrets/AzureDataLakeProjectTest.json";

    [Fact(Explicit = true)]
    public void GetProjects_ReturnsTestProject()
    {
        var dataStore = LiveTestConfiguration.GetDataStoreOrSkip(ConfigPath);
        var sut = new AzureDataLakeCrawler(
            dataStore.AccountName!,
            dataStore.TenantId!,
            dataStore.ClientId!,
            dataStore.ClientSecret!,
            dataStore.AzureFileSystemName!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        var project = Assert.Single(actual);
        Assert.Equal("TestProject", project.Name);
    }
}