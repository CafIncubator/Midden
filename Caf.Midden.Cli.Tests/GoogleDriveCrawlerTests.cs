using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Caf.Midden.Cli.Tests
{
    public class GoogleDriveCrawlerTests
    {
        private readonly CliConfiguration? _config;

        /// <summary>
        /// These tests require `GoogleDriveProjectTest.json` file to be defined in `Assets/CliConfigurationSecrets`, included in the project, with proper formatting (conforms to CliConfiguration json), and at least one .midden file in the Google Drive that the configuration file points to 
        /// Also, requires a 'DESCRIPTION.md' file in the Google Drive with project = ProductionProject
        /// </summary>
        public GoogleDriveCrawlerTests()
        {
            var configService = new ConfigurationService();
            string configPath = @"Assets/CliConfigurationSecrets/GoogleDriveProjectTest.json";
            if(File.Exists(configPath))
                _config = configService.GetConfiguration(configPath);
        }

        [Fact]
        public void GetFileNames_ValidInput_Expected()
        {
            if(_config != null)
            {
                var sut = new GoogleDriveCrawler(
                _config.DataStores[0].ClientId,
                _config.DataStores[0].ClientSecret,
                _config.DataStores[0].ApplicationName);

                var actual = sut.GetFileNames();

                Assert.NotNull(actual);
            }
            else
            {
                throw new NotImplementedException();
            }
            
        }

        [Fact]
        public void GetMetadatas_ValidInput_Expected()
        {
            if (_config != null)
            {
                var parser = new MetadataParser(
                    new MetadataConverter());
                var sut = new GoogleDriveCrawler(
                _config.DataStores[0].ClientId,
                _config.DataStores[0].ClientSecret,
                _config.DataStores[0].ApplicationName);

                var actual = sut.GetMetadatas(parser);

                Assert.NotNull(actual);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void GetProjects_ValidInput_Expected()
        {
            if (_config != null)
            {
                var reader = new ProjectReader(
                    new ProjectParser());

                var sut = new GoogleDriveCrawler(
                _config.DataStores[0].ClientId,
                _config.DataStores[0].ClientSecret,
                _config.DataStores[0].ApplicationName);

                var actual = sut.GetProjects(reader);

                Assert.NotNull(actual);
                Assert.True(actual.Count > 0);
                Assert.Equal("ProductionProject", actual[0].Name);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
