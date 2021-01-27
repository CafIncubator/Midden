using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Models
{
    public class Configuration
    {
        public List<DataStore> DataStores { get; set; } = new List<DataStore>();
    }
}
