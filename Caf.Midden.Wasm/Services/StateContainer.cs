using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caf.Midden.Components;
using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;

namespace Caf.Midden.Wasm.Services
{
    public class StateContainer : IUpdateMetadata, IUpdateMessage
    {
        public string Message { get; private set; } = "v0.1.0-alpha4";

        public void SetMessage(ComponentBase source, string value)
        {
            this.Message = value;
            NotifyStateChanged(source, "Message");
        }

        public Metadata Metadata { get; private set; }

        public void SetMetadata(ComponentBase source, Metadata value)
        {
            this.Metadata = value;

            NotifyStateChanged(source, "Metadata");
        }

        public event Action<ComponentBase, string> StateChanged;

        private void NotifyStateChanged(
            ComponentBase source,
            string property) => StateChanged?.Invoke(source, property);
    }
}
