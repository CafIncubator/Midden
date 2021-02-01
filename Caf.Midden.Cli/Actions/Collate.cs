using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Actions
{
    public class Collate : Command
    {
        private readonly Configuration config;

        public Collate(
            string name, 
            string description,
            Configuration configuration) 
            : base(name, description)
        {
            this.config = configuration;

            Add(new Option<List<string>>(
                new[] { "--datastors", "-d" },
                "List of names of data stores to crawl"));
            Add(new Option<string?>(
                new[] { "--outdir", "-o" },
                "Directory to write the catalog.json file."));

            Handler = CommandHandler
                .Create<List<string>, string?, IConsole>(HandleCollate);
        }

        public void HandleCollate(
            List<string> datastores,
            string? outdir,
            IConsole console)
        {
            // Read+parse the config file
            // foreach dataStore in dataStores
            // Create a ICrawler (or something) to collage Midden files in store based on type
            // Foreach .midden file; parse (use MetadataParser?) and add to global List<Metadata>
            // Write "catalog.json" to writeDirectory

            foreach (string store in datastores)
            {
                Console.WriteLine(store);
            }

            outdir ??= ".";

            Console.WriteLine($"Will write 'catalog.json' to {outdir}");


            List<string> middenFiles = new List<string>();

            if (datastores.Count == 0)
                datastores = config.DataStores.Select(ds => ds.Name).ToList();

            foreach (string store in datastores)
            {
                var currStore = config
                    .DataStores
                    .FirstOrDefault(s => s.Name == store);

                if (currStore == null)
                {
                    Console.WriteLine($"No data store with name {store} in config file");
                    continue;
                }

                Console.WriteLine($"Crawling Data Store: {currStore.Name}");

                ICrawl crawler = null;
                switch (currStore.Type)
                {
                    // TODO: Create an ICrawler and use builder pattern
                    // TODO: Clean this up, move to private funcs
                    case DataStoreTypes.LocalFileSystem:
                        Console.WriteLine("Crawling files");
                        crawler = new LocalFileSystemCrawler(
                            currStore.LocalPath);
                        break;
                    case DataStoreTypes.AzureDataLakeGen2:
                        Console.WriteLine("Crawling data lake");
                        if(currStore.TenantId is not null &&
                            currStore.ClientId is not null &&
                            currStore.ClientSecret is not null &&
                            currStore.AzureFileSystemName is not null)
                        {
                            crawler = new AzureDataLakeCrawler(
                                currStore.TenantId,
                                currStore.ClientId,
                                currStore.ClientSecret,
                                currStore.AzureFileSystemName);
                        }

                        break;
                    default:
                        break;
                }
                var files = crawler?.GetFileNames();

                if (files != null) middenFiles.AddRange(files);

            }

            // TODO: Create unit tests instead of junk code
            foreach (var middenFile in middenFiles)
            {
                Console.WriteLine(middenFile);
            }
        }
    }
}
