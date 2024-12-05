using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Services
{
    public class AzureDataLakeCrawler : ICrawl
    {
        private const string MIDDEN_FILE_EXTENSION = ".midden";
        private const string MIPPEN_FILE_SEARCH_TERM = "DESCRIPTION.md";

        private readonly string accountName;
        private readonly string tenantId;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string fileSystemName;

        private readonly DataLakeServiceClient serviceClient;

        public AzureDataLakeCrawler(
            string accountName,
            string tenantId,
            string clientId,
            string clientSecret,
            string fileSystemName)
        {
            this.accountName = accountName;
            this.tenantId = tenantId;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.fileSystemName = fileSystemName;

            this.serviceClient = InitializeClient();
        }

        private DataLakeServiceClient InitializeClient()
        {
            TokenCredential credential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, new TokenCredentialOptions());

            string dfsUri = $"https://{accountName}.dfs.core.windows.net";

            return new DataLakeServiceClient(new Uri(dfsUri), credential);
        }

        // Gets a list of midden/mippen file names that are two levels deep from the root. 
        // Assumes directory structure is something like "root/{projectName}/{datasetName}.midden"
        public List<string> GetFileNames(string fileExtension)
        {
            DataLakeFileSystemClient fileSystemClient =
                serviceClient.GetFileSystemClient(fileSystemName);

            var names = new List<string>();

            foreach (PathItem pathItem in fileSystemClient.GetPaths())
            {
                if (pathItem.IsDirectory ?? false)
                {
                    foreach (PathItem subPathItem in fileSystemClient.GetPaths(pathItem.Name))
                    {
                        if (subPathItem.Name.Contains(fileExtension))
                        {
                            Console.WriteLine($"  In {pathItem.Name} found {subPathItem.Name}");

                            names.Add(subPathItem.Name);
                        }
                            
                    }
                }
            }

            Console.WriteLine($"Found a total of {names.Count} files");

            return names;
        }

        public List<Metadata> GetMetadatas(
            IMetadataParser parser)
        {
            List<string> fileNames = GetFileNames(MIDDEN_FILE_EXTENSION);

            List<Metadata> metadatas = new List<Metadata>();

            DataLakeFileSystemClient fileSystemClient =
                    serviceClient.GetFileSystemClient(fileSystemName);
            
            foreach (var fileName in fileNames)
            {
                try
                {
                    // Get file contents as json string
                    DataLakeFileClient fileClient =
                        fileSystemClient.GetFileClient(fileName);

                    Response<FileDownloadInfo> fileContents = fileClient.Read();

                    string json;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        fileContents.Value.Content.CopyTo(ms);
                        json = Encoding.UTF8.GetString(ms.ToArray());
                    }

                    // Parse json string and add relative path to Dataset
                    Metadata metadata = parser.Parse(json);

                    string filePath = fileClient.Path.Replace(MIDDEN_FILE_EXTENSION, "");
                    metadata.Dataset.DatasetPath = filePath;

                    metadatas.Add(metadata);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing file: {fileName}, reason: {ex}");
                }
            }

            return metadatas;
        }

        public List<Project> GetProjects(
            ProjectReader reader)
        {
            List<string> fileNames = GetFileNames(MIPPEN_FILE_SEARCH_TERM);

            List<Project> projects = new List<Project>();

            DataLakeFileSystemClient fileSystemClient =
                    serviceClient.GetFileSystemClient(fileSystemName);

            foreach (var fileName in fileNames)
            {
                // Get file contents
                DataLakeFileClient fileClient =
                    fileSystemClient.GetFileClient(fileName);

                // Try to get a project
                Project project;
                using (var stream = fileClient.OpenRead())
                {
                    project = reader.Read(stream);
                }

                if(project is not null)
                    projects.Add(project);
            }

            return projects;
        }
    }
}
