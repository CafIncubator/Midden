using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;

namespace Caf.Midden.Wasm.Services
{
    public class StateContainer
    {
        public string SchemaVersion { get; } = "v0.1.0-alpha4";
        public string LastUpdated { get; private set; } = 
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public Metadata Metadata { get; set; }
        public Metadata MetadataTwoWayBining { get; set; } = new Metadata();
        public Metadata MetadataEdit { get; set; } = new Metadata();

        public Configuration AppConfig { get; set; }
    }
}
