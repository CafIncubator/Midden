using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Models.v0_1;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Actions
{
    public class Collate : Command
    {
        private readonly CliConfiguration config;

        public Collate(
            string name, 
            string description,
            CliConfiguration? configuration) 
            : base(name, description)
        {
            // Abort if not configuration found
            if (configuration == null)
            {
                Console.WriteLine("Unable to read 'configuration.json' file. To create a configuration file, use the 'setup' action");
                return;
            }

            this.config = configuration;


            Add(new Option<List<string>>(
                new[] { "--datastores", "-d" },
                "List of names of data stores to crawl"));
            Add(new Option<bool?>(
                new[] { "--silent", "-s" },
                "Runs in silent mode without user prompt."));
            Add(new Option<string?>(
                new[] { "--outdir", "-o" },
                "Directory to write the catalog.json file."));
            

            Handler = CommandHandler
                .Create<List<string>, bool?, string?, IConsole>(HandleCollate);
        }

        private void Add(Option<string?> option, object getDefaultValue)
        {
            throw new NotImplementedException();
        }

        public void HandleCollate(
            List<string> datastores,
            bool? silent,
            string? outdir,
            IConsole console)
        {
            bool silentMode = silent ??= false;

            // Set output directory if none specified
            outdir ??= Path.Combine(
                Directory.GetCurrentDirectory(), "catalog.json");
            Console.WriteLine($"Will write output to {outdir}");

            // If no store specified then crawl them all
            if (datastores.Count == 0)
                datastores = config.DataStores.Select(ds => ds.Name).ToList();

            Console.Write($"Plannig to crawl: ");
            foreach (var datastore in datastores) Console.Write($"{datastore} ");

            // Confirm crawl should continue
            char shouldCont = 'Y';
            
            if(!silentMode)
            {
                Console.WriteLine();
                Console.WriteLine("Continue? (Y|N)");
                shouldCont = Convert.ToChar(Console.Read());
            }

            if (shouldCont != 'Y')
            {
                Console.WriteLine("Aborting...");
                return;
            }
               
            // Start crawling all specified data stores
            List<Metadata> middenMetadatas = new List<Metadata>();

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
                    case DataStoreTypes.LocalFileSystem:
                        if(currStore.Path is not null)
                        {
                            crawler = new LocalFileSystemCrawler(
                                currStore.Path);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Not enough information provided to crawl {currStore.Name}");
                        }

                        break;

                    case DataStoreTypes.AzureDataLakeGen2:
                        if(
                            currStore.AccountName is not null &&
                            currStore.TenantId is not null &&
                            currStore.ClientId is not null &&
                            currStore.ClientSecret is not null &&
                            currStore.AzureFileSystemName is not null)
                        {

                            crawler = new AzureDataLakeCrawler(
                                currStore.AccountName,
                                currStore.TenantId,
                                currStore.ClientId,
                                currStore.ClientSecret,
                                currStore.AzureFileSystemName);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Not enough information provided to crawl {currStore.Name}");
                        }

                        break;

                    case DataStoreTypes.GoogleWorkspaceSharedDrive:
                        if(
                            currStore.ClientId is not null &&
                            currStore.ClientSecret is not null &&
                            currStore.ApplicationName is not null)
                        {
                            crawler = new GoogleWorkspaceSharedDriveCrawler(
                                currStore.ClientId,
                                currStore.ClientSecret,
                                currStore.ApplicationName);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Not enough information provided to crawl {currStore.Name}");
                        }

                        break;

                    case DataStoreTypes.AzureFileShares:
                        if(
                            currStore.Uri is not null &&
                            currStore.Path is not null &&
                            currStore.SharedAccessSignature is not null)
                        {
                            crawler = new AzureFileShareCrawler(
                                currStore.Uri,
                                currStore.Path,
                                currStore.SharedAccessSignature);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Not enough information provided to crawl {currStore.Name}");
                        }

                        break;

                    default:
                        break;
                }

                var metadatas = crawler?.GetMetadatas();

                if(metadatas != null)
                {
                    AppendDataStoreNameToPath(metadatas, currStore.Name);

                    if (middenMetadatas != null) middenMetadatas.AddRange(metadatas);
                }
            }

            Catalog catalog = new Catalog()
            {
                CreationDate = DateTime.UtcNow,
                Metadatas = middenMetadatas
            };

            Console.WriteLine($"Writing output to {outdir}");
            File.WriteAllText(outdir, JsonSerializer.Serialize(catalog));
        }
    
        private void AppendDataStoreNameToPath(
            List<Metadata> metadatas,
            string dataStoreName)
        {
            string prependString = $"[{dataStoreName}]";

            foreach(Metadata metadata in metadatas)
            {
                string newPath = $"{prependString}{metadata.Dataset.DatasetPath}";
                metadata.Dataset.DatasetPath = newPath;
            }
        }
    }
}
