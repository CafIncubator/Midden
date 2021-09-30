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
        AzureDataLakeGen2,
        GithubOrganization,
        GoogleWorkspaceSharedDrive,
        FileTransferProtocol,
        Office365OneDrive,
        AzureFileShares
    }
}
