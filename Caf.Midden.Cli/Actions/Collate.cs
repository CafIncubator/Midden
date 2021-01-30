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

            Add(new Argument<List<string>>(
                "datastores", "List of names of data stores to crawl"));
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
                        crawler = new AzureDataLakeCrawler(
                            currStore.TenantId,
                            currStore.ClientId,
                            currStore.ClientSecret);
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
