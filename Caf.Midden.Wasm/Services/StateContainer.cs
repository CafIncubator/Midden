using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caf.Midden.Components.Common;
using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;

namespace Caf.Midden.Wasm.Services
{
    public class StateContainer : IHaveMetadataState, IHaveMessageState
    {
        public string SchemaVersion { get; } = "v0.1.0-alpha4";
        public string Message { get; private set; } = "";
        public string LastUpdated { get; private set; } = 
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public void SetMessage(ComponentBase source, string value)
        {
            this.Message = value;

            LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            NotifyStateChanged(source, "Message");
        }

        public Metadata Metadata { get; private set; }

        public void SetMetadata(ComponentBase source, Metadata value)
        {
            this.Metadata = value;

            LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            NotifyStateChanged(source, "Metadata");
        }

        public Metadata MetadataTwoWayBining { get; set; } = new Metadata();
        public Metadata MetadataEdit { get; set; } = new Metadata();

        public event Action<ComponentBase, string> StateChanged;

        public void NotifyStateChanged(
            ComponentBase source,
            string property) => StateChanged?.Invoke(source, property);
    }
}
