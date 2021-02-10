using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Caf.Midden.Components.Common;
using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;

namespace Caf.Midden.Wasm.Services
{
    public class StateContainer : IUpdateAppConfig, IUpdateLastUpdated
    {
        public string SchemaVersion { get; } = "v0.1.0-alpha4";
        public DateTime LastUpdated { get; private set; } = 
            DateTime.UtcNow;
        public void UpdateLastUpdated(
            ComponentBase source,
            DateTime value)
        {
            this.LastUpdated = value;
            NotifyStateChanged(source, "LastUpdated");
        }

        //public Metadata Metadata { get; set; } = new Metadata();
        //public Metadata MetadataTwoWayBining { get; set; } = new Metadata();
        public Metadata MetadataEdit { get; set; } = new Metadata();
        public void UpdateMetadataEdit(
            ComponentBase source,
            Metadata value)
        {
            this.MetadataEdit = value;
            NotifyStateChanged(source, "UpdateMetadataEdit");
        }

        public Configuration AppConfig { get; private set; }
        public void UpdateAppConfig(
            ComponentBase source, 
            Configuration value)
        {
            this.AppConfig = value;
            NotifyStateChanged(source, "AppConfig");
        }

        public event Action<ComponentBase, string> StateChanged;

        public void NotifyStateChanged(
            ComponentBase source,
            string property) => StateChanged?.Invoke(source, property);
    }
}
