using Caf.Midden.Cli.Services;

namespace Caf.Midden.Cli.Tests;

public class ConfigurationServiceTests
{
    [Fact]
    public void GetConfiguration_DuplicateDataStoreNames_ThrowsInvalidDataException()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var configPath = Path.Combine(directory.FullName, "configuration.json");
            File.WriteAllText(configPath, """
                {
                  "Version": "1.0.0",
                  "DataStores": [
                    { "Name": "Shared", "Type": "LocalFileSystem", "Path": "C:\\a" },
                    { "Name": "shared", "Type": "LocalFileSystem", "Path": "C:\\b" }
                  ]
                }
                """);

            var service = new ConfigurationService();

            var exception = Assert.Throws<InvalidDataException>(() => service.GetConfiguration(configPath));
            Assert.Contains("Shared", exception.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetConfiguration_UniqueDataStoreNames_ReturnsConfiguration()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var configPath = Path.Combine(directory.FullName, "configuration.json");
            File.WriteAllText(configPath, """
                {
                  "Version": "1.0.0",
                  "DataStores": [
                    { "Name": "First", "Type": "LocalFileSystem", "Path": "C:\\a" },
                    { "Name": "Second", "Type": "LocalFileSystem", "Path": "C:\\b" }
                  ]
                }
                """);

            var service = new ConfigurationService();

            var configuration = service.GetConfiguration(configPath);

            Assert.NotNull(configuration);
            Assert.Equal(2, configuration.DataStores.Count);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
