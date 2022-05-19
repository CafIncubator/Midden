using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Caf.Midden.Cli.Tests
{
    public class CrawlerIntegrationTests
    {
        // NOTE: These tests require configuration files that are not included in the repository. These need to be generated for each clone.
        [Fact]
        public void GetProjects_Local()
        {
            string configPath = "Assets/CliConfigurationSecrets/LocalFileSystemProjectTest.json";
            if(!File.Exists(configPath))
                throw new FileNotFoundException(configPath);

            // Gets config file, fails if not exist
            ConfigurationService configService = new ConfigurationService();
            CliConfiguration config = configService.GetConfiguration(configPath);

            DataStore dataStore = config.DataStores[0];
            LocalFileSystemCrawler sut = new LocalFileSystemCrawler(
                dataStore.Path);

            List<Core.Models.v0_2.Project> actual = sut.GetProjects(new ProjectReader(
                new ProjectParser()));

            Assert.NotNull(actual);
            Assert.Single(actual);
            Assert.Equal("ProductionProject", actual[0].Name);
        }

        [Fact]
        public void GetProjects_AzureDataLake()
        {
            string configPath = "Assets/CliConfigurationSecrets/AzureDataLakeProjectTest.json";
            if (!File.Exists(configPath))
                throw new FileNotFoundException(configPath);

            // Gets config file, fails if not exist
            ConfigurationService configService = new ConfigurationService();
            CliConfiguration config = configService.GetConfiguration(configPath);

            DataStore dataStore = config.DataStores[0];
            AzureDataLakeCrawler sut = new AzureDataLakeCrawler(
                dataStore.AccountName,
                dataStore.TenantId,
                dataStore.ClientId,
                dataStore.ClientSecret,
                dataStore.AzureFileSystemName);

            List<Core.Models.v0_2.Project> actual = sut.GetProjects(new ProjectReader(
                new ProjectParser()));

            Assert.NotNull(actual);
            Assert.Single(actual);
            Assert.Equal("TestProject", actual[0].Name);
        }

        [Fact]
        public void GetProjects_GoogleWorkspaceSharedDrive()
        {
            string configPath = "Assets/CliConfigurationSecrets/GoogleWorkspaceSharedDriveProjectTest.json";
            if (!File.Exists(configPath))
                throw new FileNotFoundException(configPath);

            // Gets config file, fails if not exist
            ConfigurationService configService = new ConfigurationService();
            CliConfiguration config = configService.GetConfiguration(configPath);

            DataStore dataStore = config.DataStores[0];
            GoogleWorkspaceSharedDriveCrawler sut = new GoogleWorkspaceSharedDriveCrawler(
                dataStore.ClientId,
                dataStore.ClientSecret,
                dataStore.ApplicationName);

            List<Core.Models.v0_2.Project> actual = sut.GetProjects(new ProjectReader(
                new ProjectParser()));

            Assert.NotNull(actual);
            Assert.True(actual.Count() > 0);
        }
    }
}
