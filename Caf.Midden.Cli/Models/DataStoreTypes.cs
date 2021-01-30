using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Models
{
    public enum DataStoreTypes
    {
        LocalFileSystem,
        AzureBlobStorage,
        AzureDataLakeGen2,
        GithubOrganization
    }
}
