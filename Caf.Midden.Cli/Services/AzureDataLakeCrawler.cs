using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Services
{
    public class AzureDataLakeCrawler : ICrawl
    {
        private readonly string tenantId;
        private readonly string clientId;
        private readonly string clientSecret;

        public AzureDataLakeCrawler(
            string tenantId,
            string clientId,
            string clientSecret)
        {
            this.tenantId = tenantId;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        public List<string> GetFileNames()
        {
            TokenCredential credential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, new TokenCredentialOptions());

            string dfsUri = "https://cafltardatalake.dfs.core.windows.net";

            var serviceClient = new DataLakeServiceClient(new Uri(dfsUri), credential);

            DataLakeFileSystemClient fileSystemClient =
                serviceClient.GetFileSystemClient("production");

            var names = new List<string>();

            foreach (PathItem projectItem in fileSystemClient.GetPaths())
            {
                if ((bool)projectItem.IsDirectory)
                {
                    foreach(PathItem datasetItem in fileSystemClient.GetPaths(projectItem.Name))
                    {
                        if (datasetItem.Name.Contains(".midden"))
                            names.Add(datasetItem.Name);
                    }
                }
            }

            return names;
        }
    }
}
