using Caf.Midden.Cli.Actions;
using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;

namespace Caf.Midden.Cli.Tests;

/// <summary>
/// Exercises collate orchestration — partial failure handling, <c>--strict</c>, and catalog
/// output — through a fake crawler factory, so no cloud account or real data store is required.
/// </summary>
public class CollateOrchestrationTests
{
    [Fact]
    public void HandleCollate_OneStoreFails_StillWritesTheOtherStoresDatasetsAndReportsPartial()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var factory = new FakeCrawlerFactory(new()
            {
                ["Broken"] = new FakeCrawler(throwOnCrawl: new IOException("network down")),
                ["Working"] = new FakeCrawler([FakeCrawler.MetadataWithPath("Raw/Good")]),
            });

            var exitCode = Run(directory, factory, strict: false, ["Broken", "Working"]);

            // A single unreachable store must not abandon the whole run, though the non-zero exit
            // still lets CI detect that the catalog is partial.
            Assert.Equal(1, exitCode);
            Assert.Contains("Raw/Good", ReadCatalog(directory));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void HandleCollate_StrictAndOneStoreFails_ReturnsFailureExitCode()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var factory = new FakeCrawlerFactory(new()
            {
                ["Broken"] = new FakeCrawler(throwOnCrawl: new IOException("network down")),
                ["Working"] = new FakeCrawler([FakeCrawler.MetadataWithPath("Raw/Good")]),
            });

            var exitCode = Run(directory, factory, strict: true, ["Broken", "Working"]);

            Assert.Equal(1, exitCode);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void HandleCollate_UnknownStoreNameRequested_SkipsItAndStillWritesTheCatalog()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var factory = new FakeCrawlerFactory(new()
            {
                ["Working"] = new FakeCrawler([FakeCrawler.MetadataWithPath("Raw/Good")]),
            });

            var exitCode = Run(directory, factory, strict: false, ["Working", "DoesNotExist"]);

            // Skipped, not fatal: the catalog is still written, with a non-zero exit flagging it.
            Assert.Equal(1, exitCode);
            Assert.Contains("Raw/Good", ReadCatalog(directory));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void HandleCollate_CrawlSucceeds_DisposesTheCrawler()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var crawler = new FakeCrawler([FakeCrawler.MetadataWithPath("Raw/Good")]);
            var factory = new FakeCrawlerFactory(new() { ["Working"] = crawler });

            Run(directory, factory, strict: false, ["Working"]);

            // Crawlers hold SDK clients and HTTP handlers, so a completed run must release them.
            Assert.True(crawler.WasDisposed);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static int Run(
        DirectoryInfo directory,
        ICrawlerFactory factory,
        bool strict,
        IReadOnlyList<string> requestedDatastores)
    {
        var configuration = new CliConfiguration
        {
            DataStores =
            [
                new DataStore { Name = "Broken", Type = DataStoreTypes.LocalFileSystem, Path = @"C:\broken" },
                new DataStore { Name = "Working", Type = DataStoreTypes.LocalFileSystem, Path = @"C:\working" },
            ],
        };

        return CollateCommand.HandleCollate(
            configuration,
            new ConfigurationService(),
            configPath: Path.Combine(directory.FullName, "configuration.json"),
            requestedDatastores: requestedDatastores,
            silent: true,
            outputPath: Path.Combine(directory.FullName, "catalog.json"),
            verbose: false,
            strict: strict,
            crawlerFactory: factory);
    }

    private static string ReadCatalog(DirectoryInfo directory) =>
        File.ReadAllText(Path.Combine(directory.FullName, "catalog.json"));
}
