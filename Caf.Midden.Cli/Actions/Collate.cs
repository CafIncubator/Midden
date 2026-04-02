using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caf.Midden.Cli.Actions;

public static class CollateCommand
{
    private static readonly JsonSerializerOptions CatalogJsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static Command Create(ConfigurationService configurationService)
    {
        var datastoresOption = new Option<string[]>("--datastores", ["-d"])
        {
            Description = "List of names of data stores to crawl.",
            AllowMultipleArgumentsPerToken = true,
        };

        var silentOption = new Option<bool>("--silent", ["-s"])
        {
            Description = "Run without prompting for confirmation.",
        };

        var outdirOption = new Option<string?>("--outdir", ["-o"])
        {
            Description = "Path to write the generated catalog JSON file.",
        };

        var command = new Command("collate", "Create a Midden catalog file from one or more data stores.");
        command.Add(datastoresOption);
        command.Add(silentOption);
        command.Add(outdirOption);
        command.SetAction(parseResult =>
        {
            CliConfiguration? configuration;

            try
            {
                configuration = configurationService.GetConfiguration();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Unable to read '{ConfigurationService.ConfigFileName}': {exception.Message}");
                return 1;
            }

            return HandleCollate(
                configuration,
                parseResult.GetValue(datastoresOption) ?? [],
                parseResult.GetValue(silentOption),
                parseResult.GetValue(outdirOption));
        });

        return command;
    }

    private static int HandleCollate(
        CliConfiguration? configuration,
        IReadOnlyList<string> requestedDatastores,
        bool silent,
        string? outputPath)
    {
        if (configuration is null)
        {
            Console.Error.WriteLine("Unable to find 'configuration.json'. Run the 'setup' command to create one in the current directory.");
            return 1;
        }

        if (configuration.DataStores.Count == 0)
        {
            Console.Error.WriteLine("The configuration file does not contain any configured data stores.");
            return 1;
        }

        var datastoresToCrawl = requestedDatastores.Count > 0
            ? requestedDatastores.ToList()
            : configuration.DataStores.Select(dataStore => dataStore.Name).ToList();

        Console.WriteLine($"Planning to crawl: {string.Join(", ", datastoresToCrawl)}");

        if (!silent && !ShouldContinue())
        {
            Console.WriteLine("Aborting...");
            return 0;
        }

        var metadataParser = new MetadataParser(new MetadataConverter());
        var projectReader = new ProjectReader(new ProjectParser());
        List<Metadata> middenMetadatas = [];
        List<Project> middenProjects = [];

        foreach (var storeName in datastoresToCrawl)
        {
            var currentStore = configuration.DataStores.FirstOrDefault(
                store => string.Equals(store.Name, storeName, StringComparison.OrdinalIgnoreCase));

            if (currentStore is null)
            {
                Console.Error.WriteLine($"No data store with name '{storeName}' exists in the configuration file.");
                continue;
            }

            if (!TryCreateCrawler(currentStore, out var crawler))
            {
                continue;
            }

            Console.WriteLine($"Crawling data store: {currentStore.Name}");

            var metadatas = crawler.GetMetadatas(metadataParser);
            AppendDataStoreNameToPath(metadatas, currentStore.Name);
            middenMetadatas.AddRange(metadatas);

            if (currentStore.ShouldCollateProjects)
            {
                middenProjects.AddRange(crawler.GetProjects(projectReader));
            }
        }

        var resolvedOutputPath = Path.GetFullPath(outputPath ?? Path.Combine(Directory.GetCurrentDirectory(), "catalog.json"));
        var outputDirectory = Path.GetDirectoryName(resolvedOutputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var catalog = new Catalog
        {
            CreationDate = DateTime.UtcNow,
            Metadatas = middenMetadatas,
            Projects = middenProjects,
        };

        File.WriteAllText(resolvedOutputPath, JsonSerializer.Serialize(catalog, CatalogJsonSerializerOptions));
        Console.WriteLine($"Wrote catalog to {resolvedOutputPath}");

        return 0;
    }

    private static bool ShouldContinue()
    {
        Console.Write("Continue? [y/N]: ");
        var response = Console.ReadLine();

        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreateCrawler(DataStore dataStore, out ICrawl crawler)
    {
        crawler = dataStore.Type switch
        {
            DataStoreTypes.LocalFileSystem when !string.IsNullOrWhiteSpace(dataStore.Path) =>
                new LocalFileSystemCrawler(dataStore.Path),
            DataStoreTypes.AzureDataLakeGen2
                when !string.IsNullOrWhiteSpace(dataStore.AccountName)
                && !string.IsNullOrWhiteSpace(dataStore.TenantId)
                && !string.IsNullOrWhiteSpace(dataStore.ClientId)
                && !string.IsNullOrWhiteSpace(dataStore.ClientSecret)
                && !string.IsNullOrWhiteSpace(dataStore.AzureFileSystemName) =>
                new AzureDataLakeCrawler(
                    dataStore.AccountName,
                    dataStore.TenantId,
                    dataStore.ClientId,
                    dataStore.ClientSecret,
                    dataStore.AzureFileSystemName),
            DataStoreTypes.GoogleDrive
                when !string.IsNullOrWhiteSpace(dataStore.ClientId)
                && !string.IsNullOrWhiteSpace(dataStore.ClientSecret)
                && !string.IsNullOrWhiteSpace(dataStore.ApplicationName) =>
                new GoogleDriveCrawler(
                    dataStore.ClientId,
                    dataStore.ClientSecret,
                    dataStore.ApplicationName),
            DataStoreTypes.GoogleWorkspaceSharedDrive
                when !string.IsNullOrWhiteSpace(dataStore.ClientId)
                && !string.IsNullOrWhiteSpace(dataStore.ClientSecret)
                && !string.IsNullOrWhiteSpace(dataStore.ApplicationName) =>
                new GoogleWorkspaceSharedDriveCrawler(
                    dataStore.ClientId,
                    dataStore.ClientSecret,
                    dataStore.ApplicationName),
            DataStoreTypes.GoogleWorkspaceSharedDrive
                when !string.IsNullOrWhiteSpace(dataStore.AuthFilePath)
                && !string.IsNullOrWhiteSpace(dataStore.ApplicationName) =>
                new GoogleWorkspaceSharedDriveCrawler(
                    dataStore.AuthFilePath,
                    dataStore.ApplicationName),
            DataStoreTypes.AzureFileShares
                when !string.IsNullOrWhiteSpace(dataStore.Uri)
                && !string.IsNullOrWhiteSpace(dataStore.Path)
                && !string.IsNullOrWhiteSpace(dataStore.SharedAccessSignature) =>
                new AzureFileShareCrawler(
                    dataStore.Uri,
                    dataStore.Path,
                    dataStore.SharedAccessSignature),
            _ => null!,
        };

        if (crawler is not null)
        {
            return true;
        }

        Console.Error.WriteLine($"The data store '{dataStore.Name}' does not have enough configuration to crawl type '{dataStore.Type}'.");
        return false;
    }

    private static void AppendDataStoreNameToPath(IEnumerable<Metadata> metadatas, string dataStoreName)
    {
        var prependString = $"[{dataStoreName}]";

        foreach (var metadata in metadatas)
        {
            metadata.Dataset.DatasetPath = $"{prependString}{metadata.Dataset.DatasetPath}";
        }
    }
}
