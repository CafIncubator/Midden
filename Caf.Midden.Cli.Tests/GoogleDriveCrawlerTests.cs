using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Tests;

public class GoogleDriveCrawlerTests
{
    private readonly CliConfiguration? config;

    public GoogleDriveCrawlerTests()
    {
        var configService = new ConfigurationService();
        const string configPath = @"Assets/CliConfigurationSecrets/GoogleDriveProjectTest.json";

        if (File.Exists(configPath))
        {
            config = configService.GetConfiguration(configPath);
        }
    }

    [Fact]
    public void GetFileNames_ValidInput_ReturnsResults()
    {
        var dataStore = GetConfiguredDataStore();
        if (dataStore is null)
        {
            return;
        }

        var sut = new GoogleDriveCrawler(dataStore.ClientId!, dataStore.ClientSecret!, dataStore.ApplicationName!);

        var actual = sut.GetFileNames(".midden");

        Assert.NotNull(actual);
    }

    [Fact]
    public void GetMetadatas_ValidInput_ReturnsResults()
    {
        var dataStore = GetConfiguredDataStore();
        if (dataStore is null)
        {
            return;
        }

        var parser = new MetadataParser(new MetadataConverter());
        var sut = new GoogleDriveCrawler(dataStore.ClientId!, dataStore.ClientSecret!, dataStore.ApplicationName!);

        var actual = sut.GetMetadatas(parser);

        Assert.NotNull(actual);
    }

    [Fact]
    public void GetProjects_ValidInput_ReturnsProductionProject()
    {
        var dataStore = GetConfiguredDataStore();
        if (dataStore is null)
        {
            return;
        }

        var reader = new ProjectReader(new ProjectParser());
        var sut = new GoogleDriveCrawler(dataStore.ClientId!, dataStore.ClientSecret!, dataStore.ApplicationName!);

        var actual = sut.GetProjects(reader);

        Assert.NotEmpty(actual);
        Assert.Equal("ProductionProject", actual[0].Name);
    }

    private DataStore? GetConfiguredDataStore() => config?.DataStores.FirstOrDefault();
}
