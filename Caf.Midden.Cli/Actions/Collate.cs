using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Security;
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

        var configOption = new Option<string?>("--config", ["-c"])
        {
            Description = "Path to the configuration file. The secret store is read from beside it.",
        };

        var verboseOption = new Option<bool>("--verbose", ["-v"])
        {
            Description = "Print full exception details, including data that is normally redacted such as SAS query strings.",
        };

        var strictOption = new Option<bool>("--strict")
        {
            Description = "Abort the run as soon as any data store fails, instead of continuing with the remaining stores.",
        };

        var command = new Command("collate", "Create a Midden catalog file from one or more data stores.");
        command.Add(datastoresOption);
        command.Add(silentOption);
        command.Add(outdirOption);
        command.Add(configOption);
        command.Add(verboseOption);
        command.Add(strictOption);
        command.SetAction(parseResult =>
        {
            var configPath = parseResult.GetValue(configOption);
            var verbose = parseResult.GetValue(verboseOption);
            CliConfiguration? configuration;

            try
            {
                configuration = configurationService.GetConfiguration(configPath);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Unable to read '{ConfigurationService.ConfigFileName}': {ExceptionSanitizer.Describe(exception, verbose)}");
                return 1;
            }

            return HandleCollate(
                configuration,
                configurationService,
                configPath,
                parseResult.GetValue(datastoresOption) ?? [],
                parseResult.GetValue(silentOption),
                parseResult.GetValue(outdirOption),
                verbose,
                parseResult.GetValue(strictOption));
        });

        return command;
    }

    /// <summary>
    /// Orchestrates the crawl. Exposed to the test project (rather than being private) so that
    /// partial-failure handling, <c>--strict</c>, collision reporting, and the run summary can be
    /// exercised with a fake <see cref="ICrawlerFactory"/> instead of live cloud accounts.
    /// </summary>
    internal static int HandleCollate(
        CliConfiguration? configuration,
        ConfigurationService configurationService,
        string? configPath,
        IReadOnlyList<string> requestedDatastores,
        bool silent,
        string? outputPath,
        bool verbose,
        bool strict,
        ICrawlerFactory? crawlerFactory = null)
    {
        crawlerFactory ??= new CrawlerFactory();

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

        // Duplicate requested names would otherwise crawl the same store twice and double its
        // contributions to the catalog.
        var datastoresToCrawl = requestedDatastores.Count > 0
            ? requestedDatastores.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : configuration.DataStores.Select(dataStore => dataStore.Name).ToList();

        Console.WriteLine($"Planning to crawl: {string.Join(", ", datastoresToCrawl)}");

        if (!silent && Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Input is redirected, so the confirmation prompt cannot be answered. Pass --silent to run without prompting.");
            return 1;
        }

        if (!silent && !ShouldContinue())
        {
            Console.WriteLine("Aborting...");
            return 0;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var metadataParser = new MetadataParser(new MetadataConverter());
        var projectReader = new ProjectReader(new ProjectParser());
        List<Metadata> middenMetadatas = [];
        List<Project> middenProjects = [];
        var storesSucceeded = 0;
        var storesFailed = 0;
        var storesSkipped = 0;

        // Opened lazily: configurations with no 'secret:' references never prompt for a password.
        using var secretResolver = new SecretResolver(
            configurationService.GetSecretStorePath(configPath),
            () => ConsolePrompt.ReadStorePassword());

        foreach (var storeName in datastoresToCrawl)
        {
            var currentStore = configuration.DataStores.FirstOrDefault(
                store => string.Equals(store.Name, storeName, StringComparison.OrdinalIgnoreCase));

            if (currentStore is null)
            {
                Console.Error.WriteLine($"No data store with name '{storeName}' exists in the configuration file.");
                storesSkipped++;

                if (strict)
                {
                    return 1;
                }

                continue;
            }

            if (!TryCreateCrawler(currentStore, secretResolver, configurationService.GetGoogleTokenStorePath(configPath), verbose, crawlerFactory, out var crawler))
            {
                storesFailed++;

                if (strict)
                {
                    return 1;
                }

                continue;
            }

            Console.WriteLine($"Crawling data store: {currentStore.Name}");

            // Disposes the crawler (e.g. DriveService and its HttpClient) as soon as this store
            // is done, rather than leaking it for the lifetime of the whole run.
            using var disposableCrawler = crawler;

            try
            {
                // A single pass filters unsafe paths and prepends the store name, rather than
                // a filtering pass followed by a second mutating pass over the same list.
                List<Metadata> metadatas = [];

                foreach (var metadata in crawler.GetMetadatas(metadataParser))
                {
                    if (!IsDatasetPathSafe(metadata, currentStore.Name))
                    {
                        continue;
                    }

                    metadata.Dataset.DatasetPath = $"[{currentStore.Name}]{metadata.Dataset.DatasetPath}";
                    metadatas.Add(metadata);
                }

                middenMetadatas.AddRange(metadatas);

                if (currentStore.ShouldCollateProjects)
                {
                    middenProjects.AddRange(crawler.GetProjects(projectReader));
                }

                storesSucceeded++;
            }
            catch (Exception exception)
            {
                // Cloud crawlers authenticate lazily on first request, so failures surface here
                // rather than at construction. One unreachable store must not abort the run,
                // unless --strict was requested.
                Console.Error.WriteLine($"Failed to crawl data store '{currentStore.Name}': {ExceptionSanitizer.Describe(exception, verbose)}");
                storesFailed++;

                if (strict)
                {
                    return 1;
                }
            }
        }

        ReportDatasetPathCollisions(middenMetadatas);

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

        WriteCatalogAtomically(resolvedOutputPath, catalog);
        Console.WriteLine($"Wrote catalog to {resolvedOutputPath}");

        stopwatch.Stop();
        Console.WriteLine(
            $"Summary: {storesSucceeded} store(s) succeeded, {storesFailed} failed, {storesSkipped} skipped. "
            + $"Found {middenMetadatas.Count} dataset(s) and {middenProjects.Count} project(s) in {stopwatch.Elapsed.TotalSeconds:F1}s.");

        // A non-zero exit lets CI detect a partial catalog even without --strict.
        return storesFailed > 0 || storesSkipped > 0 ? 1 : 0;
    }

    /// <summary>
    /// Writes the catalog to a temp file beside the destination, then moves it into place.
    /// A process crash or power loss mid-serialization can no longer leave a half-written,
    /// unparsable catalog at <paramref name="resolvedOutputPath"/>. Before writing, the
    /// serialized JSON is round-tripped back through deserialization so a catalog that
    /// <see cref="Caf.Midden.Wasm"/> cannot load is caught here instead of surfacing later.
    /// </summary>
    private static void WriteCatalogAtomically(string resolvedOutputPath, Catalog catalog)
    {
        var tempPath = $"{resolvedOutputPath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(catalog, CatalogJsonSerializerOptions);

        VerifyCatalogRoundTrips(json, catalog);

        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, resolvedOutputPath, overwrite: true);
        }
        catch
        {
            File.Delete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// Deserializes the freshly serialized catalog JSON and checks it round-trips to the same
    /// shape, so a serialization bug cannot silently produce a catalog file that consumers
    /// (such as <see cref="Caf.Midden.Wasm"/>) fail to load.
    /// </summary>
    private static void VerifyCatalogRoundTrips(string json, Catalog catalog)
    {
        var roundTripped = JsonSerializer.Deserialize<Catalog>(json, CatalogJsonSerializerOptions)
            ?? throw new InvalidOperationException("Catalog failed to round-trip: deserialization returned null.");

        if (roundTripped.Metadatas.Count != catalog.Metadatas.Count)
        {
            throw new InvalidOperationException(
                $"Catalog failed to round-trip: expected {catalog.Metadatas.Count} metadata entries, got {roundTripped.Metadatas.Count}.");
        }

        if (roundTripped.Projects.Count != catalog.Projects.Count)
        {
            throw new InvalidOperationException(
                $"Catalog failed to round-trip: expected {catalog.Projects.Count} project entries, got {roundTripped.Projects.Count}.");
        }
    }

    /// <summary>
    /// Two data stores can legitimately contribute datasets with the same relative path, which
    /// silently overwrites one entry's identity from the reader's perspective. Report it instead
    /// of letting it pass unnoticed.
    /// </summary>
    private static void ReportDatasetPathCollisions(IReadOnlyList<Metadata> metadatas)
    {
        var collisions = metadatas
            .Where(metadata => metadata.Dataset is not null)
            .GroupBy(metadata => metadata.Dataset.DatasetPath, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var collision in collisions)
        {
            Console.Error.WriteLine($"Warning: multiple datasets share the path '{collision.Key}'.");
        }
    }

    private static bool ShouldContinue()
    {
        Console.Write("Continue? [y/N]: ");
        var response = Console.ReadLine();

        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreateCrawler(
        DataStore dataStore,
        SecretResolver secretResolver,
        string googleTokenStorePath,
        bool verbose,
        ICrawlerFactory crawlerFactory,
        out ICrawl crawler)
    {
        crawler = null!;

        string? clientSecret;
        string? sharedAccessSignature;

        try
        {
            var clientSecretResolution = secretResolver.Resolve(dataStore.ClientSecret);
            var sharedAccessSignatureResolution = secretResolver.Resolve(dataStore.SharedAccessSignature);

            WarnIfLiteral(dataStore, nameof(DataStore.ClientSecret), clientSecretResolution);
            WarnIfLiteral(dataStore, nameof(DataStore.SharedAccessSignature), sharedAccessSignatureResolution);

            clientSecret = clientSecretResolution.Value;
            sharedAccessSignature = sharedAccessSignatureResolution.Value;
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or PlatformNotSupportedException)
        {
            Console.Error.WriteLine($"Unable to resolve credentials for data store '{dataStore.Name}': {ExceptionSanitizer.Describe(exception, verbose)}");
            return false;
        }

        try
        {
            crawler = crawlerFactory.Create(dataStore, clientSecret, sharedAccessSignature, googleTokenStorePath)!;
        }
        catch (Exception exception)
        {
            // Constructing a crawler authenticates and validates paths, so a misconfigured store
            // must not take down the whole run. Remaining data stores are still crawled.
            Console.Error.WriteLine($"Unable to connect to data store '{dataStore.Name}': {ExceptionSanitizer.Describe(exception, verbose)}");
            return false;
        }

        if (crawler is not null)
        {
            return true;
        }

        Console.Error.WriteLine(
            $"The data store '{dataStore.Name}' does not have enough configuration to crawl type '{dataStore.Type}'. "
            + $"Missing propert{(GetMissingProperties(dataStore).Count == 1 ? "y" : "ies")}: {string.Join(", ", GetMissingProperties(dataStore))}.");
        return false;
    }

    /// <summary>
    /// Reports exactly which required properties are missing for the data store's configured
    /// type, rather than a generic "not enough configuration" message.
    /// </summary>
    private static IReadOnlyList<string> GetMissingProperties(DataStore dataStore)
    {
        static bool IsMissing(string? value) => string.IsNullOrWhiteSpace(value);

        List<string> missing = [];

        switch (dataStore.Type)
        {
            case DataStoreTypes.LocalFileSystem:
                if (IsMissing(dataStore.Path)) missing.Add(nameof(DataStore.Path));
                break;
            case DataStoreTypes.AzureDataLakeGen2:
                if (IsMissing(dataStore.AccountName)) missing.Add(nameof(DataStore.AccountName));
                if (IsMissing(dataStore.AzureFileSystemName)) missing.Add(nameof(DataStore.AzureFileSystemName));
                break;
            case DataStoreTypes.GoogleDrive:
                if (IsMissing(dataStore.ClientId)) missing.Add(nameof(DataStore.ClientId));
                if (IsMissing(dataStore.ClientSecret)) missing.Add(nameof(DataStore.ClientSecret));
                if (IsMissing(dataStore.ApplicationName)) missing.Add(nameof(DataStore.ApplicationName));
                break;
            case DataStoreTypes.GoogleWorkspaceSharedDrive:
                if (IsMissing(dataStore.ApplicationName)) missing.Add(nameof(DataStore.ApplicationName));
                if (IsMissing(dataStore.ClientId) && IsMissing(dataStore.AuthFilePath))
                {
                    missing.Add($"{nameof(DataStore.ClientId)}/{nameof(DataStore.ClientSecret)} or {nameof(DataStore.AuthFilePath)}");
                }
                break;
            case DataStoreTypes.AzureFileShares:
                if (IsMissing(dataStore.Uri)) missing.Add(nameof(DataStore.Uri));
                if (IsMissing(dataStore.Path)) missing.Add(nameof(DataStore.Path));
                if (IsMissing(dataStore.SharedAccessSignature)) missing.Add(nameof(DataStore.SharedAccessSignature));
                break;
            default:
                missing.Add($"an implementation for data store type '{dataStore.Type}'");
                break;
        }

        return missing.Count > 0 ? missing : ["unknown"];
    }

    private static void WarnIfLiteral(DataStore dataStore, string propertyName, SecretResolution resolution)
    {
        if (resolution.Source != SecretSource.Literal)
        {
            return;
        }

        Console.Error.WriteLine(
            $"Warning: '{propertyName}' for data store '{dataStore.Name}' is stored in plain text in the configuration file. "
            + $"Run 'midden secret set {dataStore.Name}-{propertyName.ToLowerInvariant()}' to move it into the encrypted store.");
    }

    /// <summary>
    /// <c>DatasetPath</c> comes from crawled file names and is never validated. A crafted or
    /// corrupted ".midden" file could otherwise contribute a path such as "../../secrets" that
    /// escapes the store root once something downstream joins it to a base directory.
    /// </summary>
    private static bool IsDatasetPathSafe(Metadata metadata, string storeName)
    {
        var datasetPath = metadata.Dataset?.DatasetPath;

        if (string.IsNullOrEmpty(datasetPath))
        {
            return true;
        }

        if (Path.IsPathRooted(datasetPath) || datasetPath.Split(['/', '\\']).Any(segment => segment == ".."))
        {
            Console.Error.WriteLine(
                $"Skipping a dataset from '{storeName}' because its path '{datasetPath}' would escape the data store root.");
            return false;
        }

        return true;
    }
}
