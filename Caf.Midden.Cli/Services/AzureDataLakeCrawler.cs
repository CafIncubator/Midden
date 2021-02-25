using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Core.Models.v0_1;
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
        private const string FILE_EXTENSION = ".midden";

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

        // Gets a list of midden file names that are two levels deep from the root. 
        // Assumes directory structure is something like "root/{projectName}/{datasetName}.midden"
        public List<string> GetFileNames()
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
                        if (subPathItem.Name.Contains(FILE_EXTENSION))
                            names.Add(subPathItem.Name);
                    }
                }
            }

            return names;
        }

        public List<Metadata> GetMetadatas()
        {
            List<string> files = GetFileNames();

            List<Metadata> metadatas = new List<Metadata>();

            DataLakeFileSystemClient fileSystemClient =
                    serviceClient.GetFileSystemClient(fileSystemName);

            MetadataParser parser = 
                new MetadataParser(
                    new MetadataConverter());
            
            foreach (var file in files)
            {
                // Get file contents as json string
                DataLakeFileClient fileClient = 
                    fileSystemClient.GetFileClient(file);

                Response<FileDownloadInfo> fileContents = fileClient.Read();

                string json;
                using (MemoryStream ms = new MemoryStream())
                {
                    fileContents.Value.Content.CopyTo(ms);
                    json = Encoding.UTF8.GetString(ms.ToArray());
                }

                // Parse json string and add relative path to Dataset
                Metadata metadata = parser.Parse(json);

                string filePath = fileClient.Path.Replace(FILE_EXTENSION, "");
                metadata.Dataset.DatasetPath = filePath;

                metadatas.Add(metadata);
            }

            return metadatas;
        }
    }
}
