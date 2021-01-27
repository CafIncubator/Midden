using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Caf.Midden.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            var greeting = new Command("greeting", "Say hi.")
            {
                new Argument<string>("name", "Your name."),
                new Option<string?>(new[] {"--greeting", "-g" }, "The greeting to use."),
                new Option(new[] {"--verbose", "-v" }, "Show the deets.")
            };

            var collate = new Command("collate", "Create a Midden catalog file from one or more data stores.")
            {
                new Argument<List<string>>("datastores", "Comma separated names of data stores to crawl"),
                new Option<string?>(new[] {"--outdir", "-o" }, "Directory to write the catalog.json file.")
            };

            var col = new Command("col", "Create a Midden catalog file from one or more data stores.")
            {
                new Argument<string>("datastores", "Your name."),
                new Option<string?>(new[] {"--outdir", "-o" }, "The greeting to use.")
            };


            greeting.Handler = CommandHandler.Create<string, string?, bool, IConsole>(HandleGreeting);
            col.Handler = CommandHandler.Create<string, string?, IConsole>(HandleCol);
            collate.Handler = CommandHandler.Create<List<string>, string?, IConsole>(HandleCollate);

            var cmd = new RootCommand
            {
                greeting,
                collate,
                col
            };

            return cmd.Invoke(args);
        }

        static void HandleCollate(
            List<string> datastores,
            string? outdir,
            IConsole console)
        {
            // Read+parse the config file
            // foreach dataStore in dataStores
            // Create a ICrawler (or something) to collage Midden files in store based on type
            // Foreach .midden file; parse (use MetadataParser?) and add to global List<Metadata>
            // Write "catalog.json" to writeDirectory

            //var storesList = Regex.Replace(datastores, @"\s+", "").Split(",");

            foreach (string store in datastores)
            {
                Console.WriteLine(store);
            }

            outdir ??= ".";

            Console.WriteLine($"Will write 'catalog.json' to {outdir}");

            Configuration config = new Configuration()
            {
                DataStores = new List<DataStore>()
                {
                    new DataStore()
                    {
                        Name = "TestFolder",
                        Type = DataStoreTypes.LocalFileSystem,
                        LocalPath = @"C:\Users\brcarlson\Desktop\TestFiles"
                    }
                }
            };

            List<string> middenFiles = new List<string>();

            foreach(string store in datastores)
            {
                var currStore = config.DataStores.FirstOrDefault(s => s.Name == store);

                if (currStore == null)
                {
                    Console.WriteLine($"No data store with name {store} in config file");
                    continue;
                }

                switch(currStore.Type)
                {
                    case DataStoreTypes.LocalFileSystem:
                        Console.WriteLine("Crawling files");
                        LocalFileSystemCrawler crawler =
                            new LocalFileSystemCrawler();
                        if(!String.IsNullOrEmpty(currStore.LocalPath))
                        {
                            middenFiles.AddRange(
                                crawler.GetFileNames(currStore.LocalPath));
                        }
                        
                        break;
                    case DataStoreTypes.AzureBlobStorage:
                        Console.WriteLine("Crawling blobs");
                        break;
                }
                    
            }

            foreach(var middenFile in middenFiles)
            {
                Console.WriteLine(middenFile);
            }
        }

        static void HandleGreeting(
            string name,
            string? greeting, 
            bool verbose, 
            IConsole console)
        {
            if (verbose)
                console.Out.WriteLine($"About to say hi to '{name}'...");

            greeting ??= "Hi";
            console.Out.WriteLine($"{greeting} {name}!");

            if (verbose)
                console.Out.WriteLine($"All done!");
        }

        static void HandleCol(
            string datastores,
            string? outdir,
            IConsole console)
        {
            var storesList = Regex.Replace(datastores, @"\s+", "").Split(",");

            outdir ??= "Hi";

            foreach(string store in storesList)
                console.Out.WriteLine($"{outdir} {store}!");
        }
    }
}
