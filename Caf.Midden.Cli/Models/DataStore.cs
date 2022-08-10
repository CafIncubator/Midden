using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Models
{
    public class DataStore
    {
        public string Name { get; set; } = "";
        public DataStoreTypes Type { get; set; }
        public string? Path { get; set; }
        public string? TenantId { get; set; }
        public string? AccountName { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? AzureFileSystemName { get; set; }
        public string? ApplicationName { get; set; }
        public string? SharedAccessSignature { get; set; }
        public string? Uri { get; set; }
        public bool? ShouldCollateProjects { get; set; }
        // Used for authentication methods that can be configured using a file, like a json file. Google allows this with their Serive Accounts, for example
        public string? AuthFilePath { get; set; }
    }
}
