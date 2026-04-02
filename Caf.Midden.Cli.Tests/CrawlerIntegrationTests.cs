using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;

namespace Caf.Midden.Cli.Tests;

public class CrawlerIntegrationTests
{
    [Fact]
    public void GetProjects_Local_ReturnsProductionProject()
    {
        var dataStore = GetConfiguredDataStore("Assets/CliConfigurationSecrets/LocalFileSystemProjectTest.json");
        if (dataStore is null)
        {
            return;
        }

        var sut = new LocalFileSystemCrawler(dataStore.Path!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.Single(actual);
        Assert.Equal("ProductionProject", actual[0].Name);
    }

    [Fact]
    public void GetProjects_AzureDataLake_ReturnsTestProject()
    {
        var dataStore = GetConfiguredDataStore("Assets/CliConfigurationSecrets/AzureDataLakeProjectTest.json");
        if (dataStore is null)
        {
            return;
        }

        var sut = new AzureDataLakeCrawler(
            dataStore.AccountName!,
            dataStore.TenantId!,
            dataStore.ClientId!,
            dataStore.ClientSecret!,
            dataStore.AzureFileSystemName!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.Single(actual);
        Assert.Equal("TestProject", actual[0].Name);
    }

    [Fact]
    public void GetProjects_GoogleWorkspaceSharedDrive_ReturnsProjects()
    {
        var dataStore = GetConfiguredDataStore("Assets/CliConfigurationSecrets/GoogleWorkspaceSharedDriveProjectTest.json");
        if (dataStore is null)
        {
            return;
        }

        var sut = new GoogleWorkspaceSharedDriveCrawler(
            dataStore.ClientId!,
            dataStore.ClientSecret!,
            dataStore.ApplicationName!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.NotEmpty(actual);
    }

    [Fact]
    public void GetProjects_GoogleWorkspaceSharedDriveServiceAccount_ReturnsProjects()
    {
        var dataStore = GetConfiguredDataStore("Assets/CliConfigurationSecrets/GoogleWorkspaceSharedDriveProjectTestWithServiceAccount.json");
        if (dataStore is null)
        {
            return;
        }

        var sut = new GoogleWorkspaceSharedDriveCrawler(dataStore.AuthFilePath!, dataStore.ApplicationName!);

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.NotEmpty(actual);
    }

    private static DataStore? GetConfiguredDataStore(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        var configurationService = new ConfigurationService();
        var config = configurationService.GetConfiguration(configPath);
        return config?.DataStores.FirstOrDefault();
    }
}
